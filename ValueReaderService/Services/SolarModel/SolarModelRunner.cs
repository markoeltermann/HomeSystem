using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace ValueReaderService.Services.SolarModel;

public class SolarModelRunner(ILogger<DeviceReader> logger, ConfigModel configModel, PointValueStoreAdapter pointValueStoreAdapter, HomeSystemContext dbContext) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected async override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var azimuth = devicePoints.FirstOrDefault(p => p.Type == "solar-azimuth");
        var elevation = devicePoints.FirstOrDefault(p => p.Type == "solar-elevation");
        if (azimuth == null || elevation == null)
        {
            Logger.LogError("Device points for azimuth or elevation are not found");
            return null;
        }

        var lat = configModel.WeatherForecastLatitude();
        var lon = configModel.WeatherForecastLongitude();

        var t0 = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, DateTimeKind.Utc);
        var until = t0.AddDays(5);

        //var t0 = new DateTime(timestamp.Year, 5, 11, 0, 0, 0, DateTimeKind.Utc);
        //var until = t0.AddDays(9);

        var t = t0;
        var result = new List<PointValue>();
        while (t < until)
        {
            var (elevationAngle, azimuthAngle) = SolarAngleCalculator.CalculateSolarAngles((double)lat, (double)lon, t);
            result.Add(new PointValue(elevation, elevationAngle.ToString("0.00", InvariantCulture), t));
            result.Add(new PointValue(azimuth, azimuthAngle.ToString("0.00", InvariantCulture), t));
            t = t.AddMinutes(5);
        }

        var solarPredictions = await GenerateSolarPredictions(timestamp);
        result.AddRange(solarPredictions);

        return result;
    }

    protected async Task<List<PointValue>> GenerateSolarPredictions(DateTime timestamp)
    {
        var tomorrowLocal = timestamp.ToLocalTime().Date.AddDays(1);

        var start = DateOnly.FromDateTime(timestamp.AddDays(-365));
        var end = DateOnly.FromDateTime(timestamp.AddDays(2));

        var devices = dbContext.Devices.Where(x => x.Type == "solar_model" || x.Type == "deye_inverter" || x.Type == "yrno_weather_forecast")
            .Include(x => x.DevicePoints)
            .ToList();

        var devicePoints = devices.SelectMany(x => x.DevicePoints).ToList();

        var predictedPVInputPowerPoint = devicePoints.First(x => x.Type == "predicted-pv-input-power");
        var predictedDayPVEnergyPoint = devicePoints.First(x => x.Type == "predicted-day-pv-energy");

        var allSolarModelPoints = await GetPoints(devicePoints, start, end);

        DetectSnowPoints(allSolarModelPoints);

        var maxSolarElevation = allSolarModelPoints.Where(x => x.Timestamp.ToLocalTime() >= tomorrowLocal).Max(x => x.SolarElevation);
        var dayCutoffElevation = maxSolarElevation * 0.5f;
        if (dayCutoffElevation > 14)
            dayCutoffElevation = 14;

        foreach (var solarModelPoint in allSolarModelPoints)
        {
            // Calculate Cos and Sin of Solar Elevation
            solarModelPoint.CosSolarElevation = (float)Math.Cos(solarModelPoint.SolarElevation * Math.PI / 180);
            solarModelPoint.SinSolarElevation = (float)Math.Sin(solarModelPoint.SolarElevation * Math.PI / 180);
            // Calculate Atmospheric Factor
            solarModelPoint.AtmosphericFactor = MathF.Pow(0.7f, 1f / solarModelPoint.SinSolarElevation);
            // Calculate Cos Relative Azimuth
            solarModelPoint.CosRelativeAzimuth = (float)Math.Cos((solarModelPoint.SolarAzimuth - 230) * Math.PI / 180);
        }
        var solarModelPoints = allSolarModelPoints.Where(x => x.AllValuesExist(true) && x.SolarElevation >= -5 && !x.ArePanelsUnderSnow).ToList();
        var trainPointsMorning = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false) && x.SolarElevation < dayCutoffElevation + 1 && x.SolarAzimuth < 180).ToList();
        var trainPointsEvening = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false) && x.SolarElevation < dayCutoffElevation + 1 && x.SolarAzimuth > 180).ToList();
        var trainPoints = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false) && x.SolarElevation > 6).ToList();

        var predictionPoints = solarModelPoints.Where(x => x.Timestamp.ToLocalTime() >= tomorrowLocal).OrderBy(x => x.Timestamp).ToList();
        var skippedSolarModelPoints = allSolarModelPoints.Where(x => x.Timestamp.ToLocalTime() >= tomorrowLocal).Except(predictionPoints).ToList();

        var mlContext = new MLContext();

        var inputPropertyNames = SolarModelPoint.PointProperties.Where(x => x.Key != "pv-input-power"
                && x.Key != "pv-input-power-string-1"
                && x.Key != "pv-input-power-string-2"
                && x.Key != "solar-sell-enable"
                )
            .Select(x => x.Value.Name)
            .Concat([
                nameof(SolarModelPoint.CosSolarElevation),
                nameof(SolarModelPoint.SinSolarElevation),
                nameof(SolarModelPoint.AtmosphericFactor),
                nameof(SolarModelPoint.CosRelativeAzimuth),
            ])
            .ToList();
        var outputPropertyName = nameof(SolarModelPoint.PVInputPower);

        var order = 3;
        var (generatedNames, generatedType) = GeneratePolynomialModelType<SolarModelPoint>(inputPropertyNames, outputPropertyName, order);

        var generatedTrainPoints = GeneratePolynomialModel(trainPoints, inputPropertyNames, outputPropertyName, generatedType, order);
        var generatedTrainPointsMorning = GeneratePolynomialModel(trainPointsMorning, inputPropertyNames, outputPropertyName, generatedType, order);
        var generatedTrainPointsEvening = GeneratePolynomialModel(trainPointsEvening, inputPropertyNames, outputPropertyName, generatedType, order);
        var generatedPredictionPoints = GeneratePolynomialModel(predictionPoints, inputPropertyNames, outputPropertyName, generatedType, order);

        var trainingData = CreateDataView(mlContext, generatedTrainPoints);
        var trainingDataMorning = CreateDataView(mlContext, generatedTrainPointsMorning);
        var trainingDataEvening = CreateDataView(mlContext, generatedTrainPointsEvening);
        var predictionData = CreateDataView(mlContext, generatedPredictionPoints);

        var options = new OlsTrainer.Options
        {
            LabelColumnName = nameof(SolarModelPoint.PVInputPower),
            L2Regularization = 1e-3f,
        };

        // 2. Specify data preparation and model training pipeline.
        var pipeline = mlContext.Transforms.Concatenate("Features", generatedNames.ToArray())
            .Append(mlContext.Transforms.NormalizeLogMeanVariance("Features"))
            .Append(mlContext.Regression.Trainers.Ols(options));

        // 3. Train model.
        var model = pipeline.Fit(trainingData);
        var modelMorning = pipeline.Fit(trainingDataMorning);
        var modelEvening = pipeline.Fit(trainingDataEvening);

        var trainPredictions = model.Transform(trainingData);

        var predictions = model.Transform(predictionData);
        var predictionsMoring = modelMorning.Transform(predictionData);
        var predictionsEvening = modelEvening.Transform(predictionData);
        var predictedPVInputPowers = predictions.GetColumn<float>("Score").ToArray();
        var predictedPVInputPowersMorning = predictionsMoring.GetColumn<float>("Score").ToArray();
        var predictedPVInputPowersEvening = predictionsEvening.GetColumn<float>("Score").ToArray();

        //var metrics = mlContext.Regression.Evaluate(trainPredictions, labelColumnName: nameof(SolarModelPoint.PVInputPower));
        //PrintMetrics(metrics);

        var predictedPVInputPowerPointValues = new List<PointValue>();
        var predictedDayPVEnergyPointValues = new List<(PointValue pointValue, float value)>();
        var predictedDayPVEnergy = 0f;
        DateTime? currentDate = null;

        for (int i = 0; i < predictionPoints.Count; i++)
        {
            var solarModelPoint = predictionPoints[i];

            if (currentDate != null && currentDate.Value.Date != solarModelPoint.Timestamp.ToLocalTime().Date)
            {
                predictedDayPVEnergy = 0;
            }

            currentDate = solarModelPoint.Timestamp;

            var dayCoeff = Math.Clamp(solarModelPoint.SolarElevation - dayCutoffElevation + 1.5f, 0, 3) * 0.3333333f;
            var morningEveningCoeff = 1.0f - dayCoeff;

            var predictedPVInputPower = dayCoeff * predictedPVInputPowers[i]
                + morningEveningCoeff * (solarModelPoint.SolarAzimuth < 180 ? predictedPVInputPowersMorning[i] : predictedPVInputPowersEvening[i]);

            if (predictedPVInputPower < 0)
                predictedPVInputPower = 0;
            if (predictedPVInputPower > 9000)
                predictedPVInputPower = 9000;

            predictedDayPVEnergy += predictedPVInputPower / 6000f;

            var pointValue = new PointValue(predictedPVInputPowerPoint, predictedPVInputPower.ToString("0.00", InvariantCulture), solarModelPoint.Timestamp);
            predictedPVInputPowerPointValues.Add(pointValue);

            var dayEnergyPointValue = new PointValue(predictedDayPVEnergyPoint, predictedDayPVEnergy.ToString("0.00", InvariantCulture), solarModelPoint.Timestamp);
            predictedDayPVEnergyPointValues.Add((dayEnergyPointValue, predictedDayPVEnergy));
        }

        foreach (var solarModelPoint in skippedSolarModelPoints)
        {
            predictedPVInputPowerPointValues.Add(new PointValue(predictedPVInputPowerPoint, 0.0.ToString("0.00", InvariantCulture), solarModelPoint.Timestamp));
        }

        foreach (var day in skippedSolarModelPoints.GroupBy(x => x.Timestamp.ToLocalTime().Date))
        {
            var dayMaxValue = predictedDayPVEnergyPointValues
                .Where(x => x.pointValue.Timestamp!.Value.ToLocalTime().Date == day.Key)
                .Max(x => (float?)x.value) ?? 0f;

            foreach (var dayValue in day)
            {
                var value = dayValue.Timestamp.ToLocalTime().Hour < 12 ? 0.0f : dayMaxValue;
                var dayEnergyPointValue = new PointValue(predictedDayPVEnergyPoint, value.ToString("0.00", InvariantCulture), dayValue.Timestamp);
                predictedDayPVEnergyPointValues.Add((dayEnergyPointValue, value));
            }
        }

        predictedPVInputPowerPointValues = predictedPVInputPowerPointValues.OrderBy(x => x.Timestamp).ToList();
        predictedDayPVEnergyPointValues = predictedDayPVEnergyPointValues.OrderBy(x => x.pointValue.Timestamp).ToList();

        return predictedPVInputPowerPointValues.Concat(predictedDayPVEnergyPointValues.Select(x => x.pointValue)).ToList();
    }

    private static void DetectSnowPoints(List<SolarModelPoint> allSolarModelPoints)
    {
        var days = allSolarModelPoints.GroupBy(x => x.Timestamp.ToLocalTime().Date).ToList();

        foreach (var day in days)
        {
            var maxPower = day.Max(x => x.PVInputPower);
            if (maxPower < 220)
            {
                foreach (var solarModelPoint in day)
                {
                    solarModelPoint.ArePanelsUnderSnow = true;
                }
            }
        }
    }

    private static IDataView CreateDataView(MLContext mlContext, List<object> generatedModelPoints)
    {
        // generatedModelPoints is a List<object> where each object is of the same dynamic type
        var generatedType = generatedModelPoints.First().GetType();

        // Create a strongly-typed List<T> using reflection
        var listType = typeof(List<>).MakeGenericType(generatedType);
        var typedList = Activator.CreateInstance(listType)!;

        // Add each object to the strongly-typed list
        var addMethod = listType.GetMethod("Add")!;
        foreach (var obj in generatedModelPoints)
        {
            addMethod.Invoke(typedList, new[] { obj });
        }

        // Prepare the LoadFromEnumerable<T> method
        var loadFromEnumerableMethod = typeof(DataOperationsCatalog)
            .GetMethods()
            .First(m => m.Name == "LoadFromEnumerable" && m.IsGenericMethod && m.GetParameters().Length == 2);

        var genericMethod = loadFromEnumerableMethod.MakeGenericMethod(generatedType);

        // Call: mlContext.Data.LoadFromEnumerable<T>(IEnumerable<T> data)
        var trainingData = (IDataView)genericMethod.Invoke(
            mlContext.Data,
            new object?[] { typedList, null }
        )!;
        return trainingData;
    }

    private async Task<List<SolarModelPoint>> GetPoints(List<DevicePoint> devicePoints, DateOnly start, DateOnly end)
    {
        var result = new Dictionary<DateTime, SolarModelPoint>();

        foreach (var (pointType, propertyInfo) in SolarModelPoint.PointProperties)
        {
            var pointValues = await pointValueStoreAdapter.Get(devicePoints.First(x => x.Type == pointType).Id, start, end);
            foreach (var pointValue in pointValues.Values!)
            {
                if (!result.TryGetValue(pointValue.Timestamp.UtcDateTime, out var solarModelPoint))
                {
                    solarModelPoint = new SolarModelPoint { Timestamp = pointValue.Timestamp.UtcDateTime };
                    result[solarModelPoint.Timestamp] = solarModelPoint;
                }

                propertyInfo.SetValue(solarModelPoint, (float?)pointValue.Value ?? float.NaN);
            }
        }

        return result.Values.OrderBy(x => x.Timestamp).ToList();
    }

    private static List<object> GeneratePolynomialModel<T>(List<T> inputObjects, List<string> inputPropertyNames, string outputPropertyName, Type modelType, int order)
    {
        var typeofT = typeof(T);
        var generatedTypeProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(x => x.Name);
        var inputTypeProperties = typeofT.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(x => x.Name);

        var generatedObjects = new List<object>();

        foreach (var item in inputObjects)
        {
            var generatedObject = Activator.CreateInstance(modelType)!;
            GeneratePolynomialModelValues(inputPropertyNames, generatedTypeProperties, inputTypeProperties, item, generatedObject, 0, 1, order, null, 1f);
            modelType.GetProperty(outputPropertyName)!.SetValue(generatedObject, (float)typeofT.GetProperty(outputPropertyName)!.GetValue(item)!);

            generatedObjects.Add(generatedObject);
        }

        return generatedObjects;
    }

    private static void GeneratePolynomialModelValues<T>(
        List<string> inputPropertyNames,
        Dictionary<string, PropertyInfo> generatedTypeProperties,
        Dictionary<string, PropertyInfo> inputTypeProperties,
        T item,
        object generatedObject,
        int from,
        int order,
        int maxOrder,
        string? nameBase,
        float baseValue)
    {
        for (int i = from; i < inputPropertyNames.Count; i++)
        {
            var namei = inputPropertyNames[i];
            var name = $"{(nameBase != null ? nameBase + "_" : "")}X{i}";
            var value = baseValue * (float)inputTypeProperties[namei].GetValue(item)!;
            generatedTypeProperties[name].SetValue(generatedObject, value);
            if (order < maxOrder)
            {
                GeneratePolynomialModelValues(inputPropertyNames, generatedTypeProperties, inputTypeProperties, item, generatedObject, i, order + 1, maxOrder, name, value);
            }
        }
    }

    private static (List<string> generatedNames, Type generatedType) GeneratePolynomialModelType<T>(List<string> inputPropertyNames, string outputPropertyName, int order)
    {
        var typeofT = typeof(T);

        // Define a dynamic assembly and module
        var aName = new AssemblyName("DynamicAssembly");
        var ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
        var mb = ab.DefineDynamicModule(aName.Name!);

        // Define a public class named "Person"
        var tb = mb.DefineType(typeofT.Name + "_PolynomialMembers", TypeAttributes.Public);

        var generatedNames = new List<string>();
        GenerateInputProperties(inputPropertyNames, tb, generatedNames, 0, 1, order, null);

        CreateProperty(tb, outputPropertyName, typeof(float));

        // Create the type
        var generatedType = tb.CreateType();

        return (generatedNames, generatedType);
    }

    private static void GenerateInputProperties(List<string> inputPropertyNames, TypeBuilder tb, List<string> generatedNames, int from, int order, int maxOrder, string? nameBase)
    {
        for (int i = from; i < inputPropertyNames.Count; i++)
        {
            //var name = inputPropertyNames[i];
            var name = $"{(nameBase != null ? nameBase + "_" : "")}X{i}";
            CreateProperty(tb, name, typeof(float));
            generatedNames.Add(name);
            if (order < maxOrder)
            {
                GenerateInputProperties(inputPropertyNames, tb, generatedNames, i, order + 1, maxOrder, name);
            }
        }
    }

    private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
    {
        // Define a private field
        FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

        // Define the public property
        PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

        // Define the 'get' method
        MethodBuilder getMethodBuilder = tb.DefineMethod(
            "get_" + propertyName,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            propertyType,
            Type.EmptyTypes);

        ILGenerator getIL = getMethodBuilder.GetILGenerator();
        getIL.Emit(OpCodes.Ldarg_0);
        getIL.Emit(OpCodes.Ldfld, fieldBuilder);
        getIL.Emit(OpCodes.Ret);

        // Define the 'set' method
        MethodBuilder setMethodBuilder = tb.DefineMethod(
            "set_" + propertyName,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null,
            new Type[] { propertyType });

        ILGenerator setIL = setMethodBuilder.GetILGenerator();
        setIL.Emit(OpCodes.Ldarg_0);
        setIL.Emit(OpCodes.Ldarg_1);
        setIL.Emit(OpCodes.Stfld, fieldBuilder);
        setIL.Emit(OpCodes.Ret);

        // Map the get/set methods to the property
        propertyBuilder.SetGetMethod(getMethodBuilder);
        propertyBuilder.SetSetMethod(setMethodBuilder);
    }

    private static void PrintMetrics(RegressionMetrics metrics)
    {
        Debug.WriteLine("Mean Absolute Error: " + metrics.MeanAbsoluteError);
        Debug.WriteLine("Mean Squared Error: " + metrics.MeanSquaredError);
        Debug.WriteLine(
            "Root Mean Squared Error: " + metrics.RootMeanSquaredError);

        Debug.WriteLine("RSquared: " + metrics.RSquared);
    }

    private class SolarModelPoint
    {
        public DateTime Timestamp { get; set; }

        [PointType("pv-input-power")]
        public float PVInputPower { get; set; }

        [PointType("solar-sell-enable")]
        public float SolarSellEnable { get; set; }

        [PointType("cloud-area-fraction-low")]
        public float CloudAreaFractionLow { get; set; }

        [PointType("cloud-area-fraction-medium")]
        public float CloudAreaFractionMedium { get; set; }

        [PointType("cloud-area-fraction-high")]
        public float CloudAreaFractionHigh { get; set; }

        [PointType("solar-azimuth")]
        public float SolarAzimuth { get; set; }

        [PointType("solar-elevation")]
        public float SolarElevation { get; set; }

        public float CosSolarElevation { get; set; }
        public float SinSolarElevation { get; set; }
        public float AtmosphericFactor { get; set; }
        public float CosRelativeAzimuth { get; set; }

        public bool ArePanelsUnderSnow { get; set; }

        public static readonly Dictionary<string, PropertyInfo> PointProperties = typeof(SolarModelPoint).GetProperties()
            .Where(x => x.GetCustomAttributes<PointTypeAttribute>().Any())
            .ToDictionary(x => x.GetCustomAttribute<PointTypeAttribute>()!.Type, x => x);

        public bool AllValuesExist(bool onlyInput)
        {
            IEnumerable<PropertyInfo> values = PointProperties.Values;
            if (onlyInput)
            {
                values = values.Where(x => x.Name != nameof(PVInputPower) && x.Name != nameof(SolarSellEnable));
            }
            return values.All(x => !float.IsNaN((float)x.GetValue(this)!));
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    private class PointTypeAttribute(string type) : Attribute
    {
        public string Type { get; set; } = type;
    }
}
