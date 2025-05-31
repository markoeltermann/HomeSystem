using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using PointValueStoreClient.Models;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using ValueReaderService.Services;

namespace TestApp;
public class SolarModelPOC(PointValueStoreAdapter pointValueStoreAdapter, HomeSystemContext dbContext) : BackgroundService
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;

        var start = DateOnly.FromDateTime(now.AddDays(-45));
        var end = DateOnly.FromDateTime(now.AddDays(2));

        var devices = dbContext.Devices.Where(x => x.Type == "solar_model" || x.Type == "deye_inverter" || x.Type == "yrno_weather_forecast")
            .Include(x => x.DevicePoints)
            .ToList();

        var devicePoints = devices.SelectMany(x => x.DevicePoints).ToList();

        var predictedPVInputPowerPoint = devicePoints.First(x => x.Type == "predicted-pv-input-power");
        var test1Point = devicePoints.First(x => x.Type == "solar-model-test-1");
        var test2Point = devicePoints.First(x => x.Type == "solar-model-test-2");

        var allSolarModelPoints = await GetPoints(devicePoints, start, end);
        foreach (var solarModelPoint in allSolarModelPoints)
        {
            // Calculate Cos and Sin of Solar Elevation
            solarModelPoint.CosSolarElevation = (float)Math.Cos(solarModelPoint.SolarElevation * Math.PI / 180);
            solarModelPoint.SinSolarElevation = (float)Math.Sin(solarModelPoint.SolarElevation * Math.PI / 180);
            // Calculate Atmospheric Factor
            solarModelPoint.AtmosphericFactor = MathF.Pow(0.7f, 1f / solarModelPoint.SinSolarElevation);
            // Calculate Cos Relative Azimuth
            solarModelPoint.CosRelativeAzimuth = (float)Math.Cos((solarModelPoint.SolarAzimuth - 230) * Math.PI / 180);

            //solarModelPoint.CloudAreaFractionLow = MathF.Pow(solarModelPoint.CloudAreaFractionLow, 2);
        }
        var solarModelPoints = allSolarModelPoints.Where(x => x.AllValuesExist(true) && x.SolarElevation >= -5).ToList();
        var trainPointsMorning = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false) && x.SolarElevation < 15 && x.SolarAzimuth < 180).ToList();
        var trainPointsEvening = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false) && x.SolarElevation < 15 && x.SolarAzimuth > 180).ToList();
        var trainPoints = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false) && x.SolarElevation > 6).ToList();
        //var trainPointsHigh = solarModelPoints.Where(x => x.SolarSellEnable > 0 && x.AllValuesExist(false)).ToList();
        var skippedSolarModelPoints = allSolarModelPoints.Except(solarModelPoints).ToList();

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
        var generatedFullModelPoints = GeneratePolynomialModel(solarModelPoints, inputPropertyNames, outputPropertyName, generatedType, order);

        var trainingData = CreateDataView(mlContext, generatedTrainPoints);
        var trainingDataMorning = CreateDataView(mlContext, generatedTrainPointsMorning);
        var trainingDataEvening = CreateDataView(mlContext, generatedTrainPointsEvening);
        var fullData = CreateDataView(mlContext, generatedFullModelPoints);

        var options = new OlsTrainer.Options
        {
            LabelColumnName = nameof(SolarModelPoint.PVInputPower),
            L2Regularization = 1e-3f,
        };

        // 2. Specify data preparation and model training pipeline.
        //var pipeline = mlContext.Transforms.Concatenate("Features",
        //    SolarModelPoint.PointProperties.Where(x => x.Key != "pv-input-power" && x.Key != "solar-sell-enable").Select(x => x.Value.Name).ToArray())
        //    .Append(mlContext.Transforms.NormalizeLogMeanVariance("Features"))
        //    .Append(mlContext.Regression.Trainers.Ols(labelColumnName: nameof(SolarModelPoint.PVInputPower)));
        var pipeline = mlContext.Transforms.Concatenate("Features", generatedNames.ToArray())
            .Append(mlContext.Transforms.NormalizeLogMeanVariance("Features"))
            .Append(mlContext.Regression.Trainers.Ols(options));

        // 3. Train model.
        var model = pipeline.Fit(trainingData);
        var modelMorning = pipeline.Fit(trainingDataMorning);
        var modelEvening = pipeline.Fit(trainingDataEvening);

        // ----------------

        //var (generatedModelPoints, generatedNames) = GenerateManualModel(solarModelPoints);

        //var trainingData = mlContext.Data.LoadFromEnumerable(generatedModelPoints);

        //var options = new OlsTrainer.Options
        //{
        //    LabelColumnName = nameof(OlsModelPoint.PVInputPower),
        //    L2Regularization = 1e-3f,
        //};

        //// 2. Specify data preparation and model training pipeline.
        ////var pipeline = mlContext.Transforms.Concatenate("Features",
        ////    SolarModelPoint.PointProperties.Where(x => x.Key != "pv-input-power" && x.Key != "solar-sell-enable").Select(x => x.Value.Name).ToArray())
        ////    .Append(mlContext.Transforms.NormalizeLogMeanVariance("Features"))
        ////    .Append(mlContext.Regression.Trainers.Ols(labelColumnName: nameof(SolarModelPoint.PVInputPower)));
        //var pipeline = mlContext.Transforms.Concatenate("Features", generatedNames.ToArray())
        //    .Append(mlContext.Transforms.NormalizeLogMeanVariance("Features"))
        //    .Append(mlContext.Regression.Trainers.Ols(options));

        //// 3. Train model.
        //var model = pipeline.Fit(trainingData);

        var trainPredictions = model.Transform(trainingData);

        var predictions = model.Transform(fullData);
        var predictionsMoring = modelMorning.Transform(fullData);
        var predictionsEvening = modelEvening.Transform(fullData);
        var predictedPVInputPowers = predictions.GetColumn<float>("Score").ToArray();
        var predictedPVInputPowersMorning = predictionsMoring.GetColumn<float>("Score").ToArray();
        var predictedPVInputPowersEvening = predictionsEvening.GetColumn<float>("Score").ToArray();

        var metrics = mlContext.Regression.Evaluate(trainPredictions, labelColumnName: nameof(SolarModelPoint.PVInputPower));
        PrintMetrics(metrics);

        var predictedPVInputPowerPointValues = new List<NumericValueDto>();
        for (int i = 0; i < solarModelPoints.Count; i++)
        {
            var solarModelPoint = solarModelPoints[i];
            var predictedPVInputPower = solarModelPoint.SolarElevation < 14
                ? (solarModelPoint.SolarAzimuth < 180 ? predictedPVInputPowersMorning[i] : predictedPVInputPowersEvening[i])
                : predictedPVInputPowers[i];

            //var predictedPVInputPower = predictedPVInputPowers[i];

            if (predictedPVInputPower < 0)
                predictedPVInputPower = 0;
            if (predictedPVInputPower > 9000)
                predictedPVInputPower = 9000;

            var pointValue = new NumericValueDto
            {
                Timestamp = solarModelPoint.Timestamp,
                Value = predictedPVInputPower,
            };
            predictedPVInputPowerPointValues.Add(pointValue);
        }

        foreach (var solarModelPoint in skippedSolarModelPoints)
        {
            predictedPVInputPowerPointValues.Add(new NumericValueDto
            {
                Timestamp = solarModelPoint.Timestamp,
                Value = 0.0,
            });
        }

        predictedPVInputPowerPointValues = predictedPVInputPowerPointValues.OrderBy(x => x.Timestamp).ToList();

        var body = new ValueContainerDto { Values = predictedPVInputPowerPointValues };
        await pointValueStoreAdapter.Client.Points[predictedPVInputPowerPoint.Id].Values.PutAsync(body, cancellationToken: stoppingToken);

        //var plot = new Plot();
        //var plotPoints = trainPoints.Where(x => x.SolarElevation is > 45 and <= 50).ToArray();
        //plot.Add.Markers(plotPoints.Select(x => (double)x.PVInputPower).ToArray(), plotPoints.Select(x => (double)x.CloudAreaFractionLow).ToArray());
        //plot.SavePng("plot.png", 2000, 1200);

        //await CreateAndSavePointValues(predictedPVInputPowerPoint, solarModelPoints, skippedSolarModelPoints, predictedPVInputPowers, stoppingToken);
        //await CreateAndSavePointValues(test1Point, solarModelPoints, skippedSolarModelPoints, predictedPVInputPowersMorning, stoppingToken);
        //await CreateAndSavePointValues(test2Point, solarModelPoints, skippedSolarModelPoints, predictedPVInputPowersEvening, stoppingToken);
    }

    private async Task CreateAndSavePointValues(DevicePoint predictedPVInputPowerPoint, List<SolarModelPoint> solarModelPoints, List<SolarModelPoint> skippedSolarModelPoints, float[] predictedPVInputPowers, CancellationToken stoppingToken)
    {
        var predictedPVInputPowerPointValues = new List<NumericValueDto>();
        for (int i = 0; i < solarModelPoints.Count; i++)
        {
            var solarModelPoint = solarModelPoints[i];
            //var predictedPVInputPower = solarModelPoint.SolarElevation < 9
            //    ? (solarModelPoint.SolarAzimuth < 180 ? predictedPVInputPowersMorning[i] : predictedPVInputPowersEvening[i])
            //    : predictedPVInputPowers[i];

            var predictedPVInputPower = predictedPVInputPowers[i];

            if (predictedPVInputPower < 0)
                predictedPVInputPower = 0;
            if (predictedPVInputPower > 7000)
                predictedPVInputPower = 7000;

            var pointValue = new NumericValueDto
            {
                Timestamp = solarModelPoint.Timestamp,
                Value = predictedPVInputPower,
            };
            predictedPVInputPowerPointValues.Add(pointValue);
        }

        foreach (var solarModelPoint in skippedSolarModelPoints)
        {
            predictedPVInputPowerPointValues.Add(new NumericValueDto
            {
                Timestamp = solarModelPoint.Timestamp,
                Value = null,
            });
        }

        predictedPVInputPowerPointValues = predictedPVInputPowerPointValues.OrderBy(x => x.Timestamp).ToList();

        var body = new ValueContainerDto { Values = predictedPVInputPowerPointValues };
        await pointValueStoreAdapter.Client.Points[predictedPVInputPowerPoint.Id].Values.PutAsync(body, cancellationToken: stoppingToken);
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

    private static (IEnumerable<OlsModelPoint> generatedModelPoints, List<string> generatedNames) GenerateManualModel(List<SolarModelPoint> solarModelPoints)
    {
        var result = new List<OlsModelPoint>();
        var generatedNames = new List<string>
        {
            nameof(OlsModelPoint.CloudAreaFractionLow),
            //nameof(OlsModelPoint.CloudAreaFractionLow2),
            //nameof(OlsModelPoint.CloudAreaFractionLow3),
            //nameof(OlsModelPoint.CloudAreaFractionLow4),
            nameof(OlsModelPoint.CloudAreaFractionMedium),
            //nameof(OlsModelPoint.CloudAreaFractionMedium2),
            //nameof(OlsModelPoint.CloudAreaFractionMedium3),
            //nameof(OlsModelPoint.CloudAreaFractionMedium4),
            nameof(OlsModelPoint.CloudAreaFractionHigh),
            //nameof(OlsModelPoint.CloudAreaFractionHigh2),
            //nameof(OlsModelPoint.CloudAreaFractionHigh3),
            //nameof(OlsModelPoint.CloudAreaFractionHigh4),
            nameof(OlsModelPoint.CloudAreaFraction),
            //nameof(OlsModelPoint.CloudAreaFraction2),
            //nameof(OlsModelPoint.CloudAreaFraction3),
            //nameof(OlsModelPoint.CloudAreaFraction4),
            nameof(OlsModelPoint.SolarAzimuth),
            //nameof(OlsModelPoint.SolarAzimuth2),
            //nameof(OlsModelPoint.SolarAzimuth3),
            //nameof(OlsModelPoint.SolarAzimuth4),
            nameof(OlsModelPoint.SolarElevation),
            //nameof(OlsModelPoint.SolarElevation2),
            //nameof(OlsModelPoint.SolarElevation3),
            //nameof(OlsModelPoint.SolarElevation4),
        };

        foreach (var p in solarModelPoints)
        {
            // Calculate CloudAreaFraction as the combined effect
            float cloudAreaFraction = 1f - (1f - p.CloudAreaFractionLow) * (1f - p.CloudAreaFractionMedium) * (1f - p.CloudAreaFractionHigh);

            var modelPoint = new OlsModelPoint
            {
                PVInputPower = p.PVInputPower,

                CloudAreaFractionLow = p.CloudAreaFractionLow,
                CloudAreaFractionLow2 = MathF.Pow(p.CloudAreaFractionLow, 2),
                CloudAreaFractionLow3 = MathF.Pow(p.CloudAreaFractionLow, 3),
                CloudAreaFractionLow4 = MathF.Pow(p.CloudAreaFractionLow, 4),

                CloudAreaFractionMedium = p.CloudAreaFractionMedium,
                CloudAreaFractionMedium2 = MathF.Pow(p.CloudAreaFractionMedium, 2),
                CloudAreaFractionMedium3 = MathF.Pow(p.CloudAreaFractionMedium, 3),
                CloudAreaFractionMedium4 = MathF.Pow(p.CloudAreaFractionMedium, 4),

                CloudAreaFractionHigh = p.CloudAreaFractionHigh,
                CloudAreaFractionHigh2 = MathF.Pow(p.CloudAreaFractionHigh, 2),
                CloudAreaFractionHigh3 = MathF.Pow(p.CloudAreaFractionHigh, 3),
                CloudAreaFractionHigh4 = MathF.Pow(p.CloudAreaFractionHigh, 4),

                CloudAreaFraction = cloudAreaFraction,
                CloudAreaFraction2 = MathF.Pow(cloudAreaFraction, 2),
                CloudAreaFraction3 = MathF.Pow(cloudAreaFraction, 3),
                CloudAreaFraction4 = MathF.Pow(cloudAreaFraction, 4),

                SolarAzimuth = p.SolarAzimuth,
                SolarAzimuth2 = MathF.Pow(p.SolarAzimuth, 2),
                SolarAzimuth3 = MathF.Pow(p.SolarAzimuth, 3),
                SolarAzimuth4 = MathF.Pow(p.SolarAzimuth, 4),

                SolarElevation = p.SolarElevation,
                SolarElevation2 = MathF.Pow(p.SolarElevation, 2),
                SolarElevation3 = MathF.Pow(p.SolarElevation, 3),
                SolarElevation4 = MathF.Pow(p.SolarElevation, 4)
            };

            result.Add(modelPoint);
        }

        return (result, generatedNames);
    }

    private async Task<List<SolarModelPoint>> GetPoints(List<DevicePoint> devicePoints, DateOnly start, DateOnly end)
    {
        var result = new Dictionary<DateTime, SolarModelPoint>();

        foreach (var (pointType, propertyInfo) in SolarModelPoint.PointProperties)
        {
            var pointValues = await pointValueStoreAdapter.Get(devicePoints.First(x => x.Type == pointType).Id, start, end);
            foreach (var pointValue in pointValues.Values!)
            {
                if (!result.TryGetValue(pointValue.Timestamp!.Value.DateTime, out var solarModelPoint))
                {
                    solarModelPoint = new SolarModelPoint { Timestamp = pointValue.Timestamp.Value.DateTime };
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

        var generatedObjects = new List<object>();

        foreach (var item in inputObjects)
        {
            var generatedObject = Activator.CreateInstance(modelType)!;
            GeneratePolynomialModelValue(inputPropertyNames, typeofT, generatedTypeProperties, item, generatedObject, 0, 1, order, null, 1f);
            modelType.GetProperty(outputPropertyName)!.SetValue(generatedObject, (float)typeofT.GetProperty(outputPropertyName)!.GetValue(item)!);

            generatedObjects.Add(generatedObject);
        }

        return generatedObjects;
    }

    private static void GeneratePolynomialModelValue<T>(
        List<string> inputPropertyNames,
        Type typeofT,
        Dictionary<string, PropertyInfo> generatedTypeProperties,
        T item,
        object generatedObject,
        int from,
        int order,
        int maxOrder,
        string? nameBase,
        float baseValue)
    {
        //for (int i = 0; i < inputPropertyNames.Count; i++)
        //{
        //    var namei = inputPropertyNames[i];
        //    generatedTypeProperties[$"X{i}"].SetValue(generatedObject, (float)typeofT.GetProperty(namei)!.GetValue(item)!);
        //    for (int j = i; j < inputPropertyNames.Count; j++)
        //    {
        //        var namej = inputPropertyNames[j];
        //        generatedTypeProperties[$"X{i}_X{j}"].SetValue(generatedObject, (float)typeofT.GetProperty(namej)!.GetValue(item)! * (float)typeofT.GetProperty(namei)!.GetValue(item)!);
        //        for (int k = j; k < inputPropertyNames.Count; k++)
        //        {
        //            var namek = inputPropertyNames[k];
        //            generatedTypeProperties[$"X{i}_X{j}_X{k}"].SetValue(generatedObject, (float)typeofT.GetProperty(namek)!.GetValue(item)! * (float)typeofT.GetProperty(namej)!.GetValue(item)! * (float)typeofT.GetProperty(namei)!.GetValue(item)!);
        //            for (int l = k; l < inputPropertyNames.Count; l++)
        //            {
        //                var namel = inputPropertyNames[l];
        //                generatedTypeProperties[$"X{i}_X{j}_X{k}_X{l}"].SetValue(generatedObject, (float)typeofT.GetProperty(namel)!.GetValue(item)! * (float)typeofT.GetProperty(namek)!.GetValue(item)! * (float)typeofT.GetProperty(namej)!.GetValue(item)! * (float)typeofT.GetProperty(namei)!.GetValue(item)!);
        //            }
        //        }
        //    }
        //}
        for (int i = from; i < inputPropertyNames.Count; i++)
        {
            var namei = inputPropertyNames[i];
            var name = $"{(nameBase != null ? nameBase + "_" : "")}X{i}";
            var value = baseValue * (float)typeofT.GetProperty(namei)!.GetValue(item)!;
            generatedTypeProperties[name].SetValue(generatedObject, value);
            if (order < maxOrder)
            {
                GeneratePolynomialModelValue(inputPropertyNames, typeofT, generatedTypeProperties, item, generatedObject, i, order + 1, maxOrder, name, value);
            }
            //for (int j = i; j < inputPropertyNames.Count; j++)
            //{
            //    var namej = inputPropertyNames[j];
            //    generatedTypeProperties[$"X{i}_X{j}"].SetValue(generatedObject, (float)typeofT.GetProperty(namej)!.GetValue(item)! * (float)typeofT.GetProperty(namei)!.GetValue(item)!);
            //    for (int k = j; k < inputPropertyNames.Count; k++)
            //    {
            //        var namek = inputPropertyNames[k];
            //        generatedTypeProperties[$"X{i}_X{j}_X{k}"].SetValue(generatedObject, (float)typeofT.GetProperty(namek)!.GetValue(item)! * (float)typeofT.GetProperty(namej)!.GetValue(item)! * (float)typeofT.GetProperty(namei)!.GetValue(item)!);
            //        for (int l = k; l < inputPropertyNames.Count; l++)
            //        {
            //            var namel = inputPropertyNames[l];
            //            generatedTypeProperties[$"X{i}_X{j}_X{k}_X{l}"].SetValue(generatedObject, (float)typeofT.GetProperty(namel)!.GetValue(item)! * (float)typeofT.GetProperty(namek)!.GetValue(item)! * (float)typeofT.GetProperty(namej)!.GetValue(item)! * (float)typeofT.GetProperty(namei)!.GetValue(item)!);
            //        }
            //    }
            //}
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

            //for (int j = i; j < inputPropertyNames.Count; j++)
            //{
            //    CreateProperty(tb, $"X{i}_X{j}", typeof(float));
            //    generatedNames.Add($"X{i}_X{j}");
            //    for (int k = j; k < inputPropertyNames.Count; k++)
            //    {
            //        CreateProperty(tb, $"X{i}_X{j}_X{k}", typeof(float));
            //        generatedNames.Add($"X{i}_X{j}_X{k}");
            //        for (int l = k; l < inputPropertyNames.Count; l++)
            //        {
            //            CreateProperty(tb, $"X{i}_X{j}_X{k}_X{l}", typeof(float));
            //            generatedNames.Add($"X{i}_X{j}_X{k}_X{l}");
            //        }
            //    }
            //}
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

    private class OlsModelPoint
    {
        public float PVInputPower { get; set; }

        public float CloudAreaFractionLow { get; set; }
        public float CloudAreaFractionLow2 { get; set; }
        public float CloudAreaFractionLow3 { get; set; }
        public float CloudAreaFractionLow4 { get; set; }

        public float CloudAreaFractionMedium { get; set; }
        public float CloudAreaFractionMedium2 { get; set; }
        public float CloudAreaFractionMedium3 { get; set; }
        public float CloudAreaFractionMedium4 { get; set; }

        public float CloudAreaFractionHigh { get; set; }
        public float CloudAreaFractionHigh2 { get; set; }
        public float CloudAreaFractionHigh3 { get; set; }
        public float CloudAreaFractionHigh4 { get; set; }

        public float CloudAreaFraction { get; set; }
        public float CloudAreaFraction2 { get; set; }
        public float CloudAreaFraction3 { get; set; }
        public float CloudAreaFraction4 { get; set; }

        public float SolarAzimuth { get; set; }
        public float SolarAzimuth2 { get; set; }
        public float SolarAzimuth3 { get; set; }
        public float SolarAzimuth4 { get; set; }

        public float SolarElevation { get; set; }
        public float SolarElevation2 { get; set; }
        public float SolarElevation3 { get; set; }
        public float SolarElevation4 { get; set; }
    }
}
