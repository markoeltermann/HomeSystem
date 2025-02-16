using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Serialization;
using MyUplinkConnector;
using TestApp;

var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location))
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Build();

//configuration.GetConnectionString("HomeSystemContext")

var optionsBuilder = new DbContextOptionsBuilder<HomeSystemContext>()
    .UseNpgsql(configuration.GetConnectionString("HomeSystemContext"));

var dbContext = new HomeSystemContext(optionsBuilder.Options);

//await ModbusRegisterProcessor.Run(dbContext);
//await DataAnalyzer.Run();
//await ConsumptionAnalyser.Run();
//return;

await YrNoModelConverter.Run(dbContext);
return;



//var bacnetClient = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0));

//bacnetClient.Start();    // go

////Send WhoIs in order to get back all the Iam responses :
////bacnetClient.OnIam += BacnetClient_OnIam;

////bacnetClient.RegisterAsForeignDevice("192.168.1.179", 60);

////bacnetClient.RemoteWhoIs("192.168.1.179");

////bacnetClient.WhoIs();

//IPHostEntry hostEntry;

//hostEntry = Dns.GetHostEntry("tew-752dru");

////you might get more than one ip for a hostname since
////DNS supports more than one record

//IPAddress ipAddress = null;

//if (hostEntry.AddressList.Length > 0)
//{
//    ipAddress = hostEntry.AddressList[0];
//}

//var address = new BacnetAddress(BacnetAddressTypes.IP, ipAddress.ToString());

//var deviceObjId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 60);
//var analogValueId = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 0);
//var objectIdList = await bacnetClient.ReadPropertyAsync(address, deviceObjId, BacnetPropertyIds.PROP_OBJECT_LIST);

////var propertyReferences = new List<BacnetPropertyReference>
////{
////    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, ASN1.BACNET_ARRAY_ALL)
////};

////bacnetClient.ReadPropertyMultipleRequest(address, analogValueId, propertyReferences, out var values);

//foreach (var objectId in objectIdList.Select(oid => (BacnetObjectId)oid.Value).Skip(1))
//{
//    //Console.WriteLine($"{objectId}");
//    //bacnetClient.ReadPropertyAsync(address, deviceObjId, BacnetPropertyIds.PROP_ALL);
//    //var propertyReferences = new List<BacnetPropertyReference>
//    //    {
//    //        new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, ASN1.BACNET_ARRAY_ALL)
//    //    };

//    var propertyReferences = new List<BacnetPropertyReference>
//        {
//            //new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, ASN1.BACNET_ARRAY_ALL),
//            //new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ASN1.BACNET_ARRAY_ALL),
//            new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_NAME, ASN1.BACNET_ARRAY_ALL),
//            new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_TYPE, ASN1.BACNET_ARRAY_ALL),
//            new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL),
//            new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_STATE_TEXT, ASN1.BACNET_ARRAY_ALL),
//        };

//    //bacnetClient.ReadPropertyMultipleRequest()
//    //var x = new BacnetReadAccessSpecification()
//    //bacnetClient.read
//    bacnetClient.ReadPropertyMultipleRequest(address, objectId, propertyReferences, out var values);
//    //var name = await bacnetClient.ReadPropertyAsync(address, objectId, BacnetPropertyIds.PROP_ALL);
//    //Console.WriteLine($"{objectId}: {name[0]}");
//    //foreach (var prop in values[0].values)
//    //{
//    //    Console.WriteLine($"\t{prop.property} :: {string.Join(", ", prop.value)}");
//    //}

//    var name = values[0].values[0].value[0].Value.ToString();
//    var objType = (uint)values[0].values[1].value[0].Value;
//    var presentValue = values[0].values[2].value[0].Value.ToString();

//    //if ((name.StartsWith("INFO") && !name.Contains("water heater") && !name.Contains("water cooler") && !name.Contains("DX unit")
//    //        && !name.Contains("water temperature") && !name.Contains("panel 2") && !name.Contains("air quality/humidity sensor") && !name.Contains("pressure"))
//    //    || (name.StartsWith("CONTROL") && !name.Contains("auto mode") && (name.Contains("mode") || name.Contains("status") || name.Contains("current"))))
//    if ((name.Contains("water") || name.Contains("cool") || name.Contains("Water heat") || name.Contains("INFO: heating")) && !name.Contains("ECO") && !name.Contains("ALARM"))
//    {
//        name = name.Replace("INFO: ", "");
//        name = name.Replace("CONTROL: ", "");
//        name = name.Replace("intensivity", "intensity");

//        name = char.ToUpper(name[0]) + name[1..];

//        //if (objType != 19)
//        //{
//        Console.WriteLine($"{name} :: {presentValue} ({objType})");

//        var p = new DevicePoint
//        {
//            //Id = -1,
//            DeviceId = 1,
//            Address = objectId.ToString(),
//            DataTypeId = objType switch
//            {
//                2 => 1,
//                5 => 3,
//                48 => 2,
//                19 => 4,
//                _ => throw new InvalidOperationException()
//            },
//            Name = name
//        };
//        //if (objType == 19)
//        //{
//        //    var enumMembers = values[0].values[3].value;
//        //    for (int i = 0; i < enumMembers.Count; i++)
//        //    {
//        //        var member = enumMembers[i];
//        //        p.EnumMembers.Add(new EnumMember
//        //        {
//        //            Name = member.Value.ToString(),
//        //            Value = i + 1
//        //        });
//        //    }
//        //}

//        dbContext.DevicePoints.Add(p);

//        dbContext.SaveChanges();
//        //}
//        //else
//        //{
//        //    Console.WriteLine($"{name} :: {presentValue} ({objType}) :: ({string.Join(", ", values[0].values[3].value)})");
//        //}
//    }
//}

//var points = dbContext.DevicePoints.ToList();

//var readCommands = points.Select(p =>
//{
//    var objectId = BacnetObjectId.Parse(p.Address);

//    return new BacnetReadAccessSpecification
//    {
//        objectIdentifier = objectId,
//        propertyReferences = new[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL) }
//    };
//}).ToList();

//bacnetClient.ReadPropertyMultipleRequest(address, readCommands, out var values);

//Console.ReadLine();

//var job = new Job
//{
//    Name = "Test",
//    StartTime = DateTime.UtcNow,
//    Status = JobStatus.Completed
//};

//dbContext.Jobs.Add(job);
//dbContext.SaveChanges();

//var devices = await dbContext.Devices.ToListAsync();
//Console.WriteLine(JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true }));

//var httpClient = new HttpClient();

////AccessTokenRequest accessTokenRequest = new()
////{
////    ClientId = "43b1934d7fa045659bbbe42bbd4a6e0a",
////    ClientSecret = "2mXt/jalJ4gxZuEf+VbH3+ZOb2QdYqGA9ZBrDHxii6M=",
////    Code = "NYxZ%21IAAAAMl5qdwRjR5eTzpk1Cfze3NAXVyWs6SofCp_GzikMYwCAQEAAAEZERqL4xLkv4Hk20daTVRk-Or0tdhrZgCgzaJUgRpoygzaDHUqu-F9mpduFIEmZT3J911ex7NjZSWVsLOZqcXvkFV9bEF9FICjjZNFvbN1C0F75ECzX5lsvEnkg0xSPq3F7v28kup2PnnrqXC2QUu3NYnDSp9lKtNBt55tEEvrxLkqb-z-oHtQ50aQGYwKpyBtJt6SXzJKw_8fBOqd1ZatLgX-6BOZCMMXbK0OgOGD8hJ6yc8G0q5DDYnu5hFVZa_PV2LQ10pHeAVwMcUusEll2WWVVwa7cuBJLd8dfeClvctPOLvLf3NsqQOLbW6zU36S2AmZt2nyP-sneR06RbAN",
////    Scope = "READSYSTEM",
////    RedirectUri = "http://www.gsiorjhieorj.com"
////};
////var requestJson = JsonSerializer.Serialize(accessTokenRequest);

////var accessTokenRequest = "grant_type=authorization_code&client_id=43b1934d7fa045659bbbe42bbd4a6e0a&client_secret=2mXt%2FjalJ4gxZuEf%2BVbH3%2BZOb2QdYqGA9ZBrDHxii6M%3D&code=NYxZ%21IAAAAMl5qdwRjR5eTzpk1Cfze3NAXVyWs6SofCp_GzikMYwCAQEAAAEZERqL4xLkv4Hk20daTVRk-Or0tdhrZgCgzaJUgRpoygzaDHUqu-F9mpduFIEmZT3J911ex7NjZSWVsLOZqcXvkFV9bEF9FICjjZNFvbN1C0F75ECzX5lsvEnkg0xSPq3F7v28kup2PnnrqXC2QUu3NYnDSp9lKtNBt55tEEvrxLkqb-z-oHtQ50aQGYwKpyBtJt6SXzJKw_8fBOqd1ZatLgX-6BOZCMMXbK0OgOGD8hJ6yc8G0q5DDYnu5hFVZa_PV2LQ10pHeAVwMcUusEll2WWVVwa7cuBJLd8dfeClvctPOLvLf3NsqQOLbW6zU36S2AmZt2nyP-sneR06RbAN&redirect_uri=http://www.gsiorjhieorj.com&scope=READSYSTEM%20WRITESYSTEM";

//var accessTokenJson = File.ReadAllText("accessToken.json");

//var accessTokenInfo = JsonSerializer.Deserialize<AccessTokenInfo>(accessTokenJson);

////Dictionary<string, string> accessTokenRequest = new()
////{
////    ["grant_type"] = "authorization_code",
////    ["client_id"] = "43b1934d7fa045659bbbe42bbd4a6e0a",
////    ["client_secret"] = "2mXt/jalJ4gxZuEf+VbH3+ZOb2QdYqGA9ZBrDHxii6M=",
////    ["code"] = "NYxZ!IAAAAPTbcKHlui7VH2IK2Zqdgdj6-AvevdyY4P4KliK-AHAoAQEAAAFR6BrJUpLwalpbSi-SruS2UHUUgcAIkvkj53gQ0cyId0Hd_wNMkB5PQZdU67wZrltv3FEL5fausq9VV-pPwOJ9ACtfOZiaEmMefmqYA2rBrp-3BMuEGG8LCdy9Aj0zZXQUY1uTxf4Kt8UsukpsiNydS46Yg7LXy09ORkr_q0PJ5CUh17FzWZYppmhBTO7E9VKwxx9s-FsqvPbtiCSWHNyP3Za7l77Hll7kTsZKrGYTAaur-fBd-2JdCKK9BM1n7oVaaf0i7uK17jBkn3SFmoqr3j4ql8Oy4lSq4LdhGhyEkftL8n_YN2om08P3DIwBtn7aJB2nC85JPwPEgElT1hLr",
////    ["redirect_uri"] = "http://www.gsiorjhieorj.com",
////    ["scope"] = "READSYSTEM WRITESYSTEM"
////};

//Dictionary<string, string> accessTokenRequest = new()
//{
//    ["grant_type"] = "refresh_token",
//    ["client_id"] = "43b1934d7fa045659bbbe42bbd4a6e0a",
//    ["client_secret"] = "2mXt/jalJ4gxZuEf+VbH3+ZOb2QdYqGA9ZBrDHxii6M=",
//    ["refresh_token"] = accessTokenInfo.RefreshToken
//};

//var content = new FormUrlEncodedContent(accessTokenRequest);
//content.Headers.ContentType.CharSet = "UTF-8";

//var contentString = await content.ReadAsStringAsync();

//var accessTokenResponse = await httpClient.PostAsync("https://api.nibeuplink.com/oauth/token", content);

//accessTokenJson = await accessTokenResponse.Content.ReadAsStringAsync();

//File.WriteAllText("accessToken.json", accessTokenJson);

//accessTokenInfo = JsonSerializer.Deserialize<AccessTokenInfo>(accessTokenJson);

//httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessTokenInfo.AccessToken);

//var systemInfo = await httpClient.GetFromJsonAsync<List<Category>?>("https://api.nibeuplink.com/api/v1/systems/158373/serviceinfo/categories?parameters=True&systemUnitId=0");
//var systemInfo2 = await httpClient.GetFromJsonAsync<Category[]?>("https://api.nibeuplink.com/api/v1/systems/158373/serviceinfo/categories?parameters=True&systemUnitId=1");
//systemInfo.AddRange(systemInfo2);

//foreach (var category in systemInfo)
//{
//    Console.WriteLine(category.Name);
//    foreach (var parameter in category.Parameters)
//    {
//        Console.WriteLine($"\t{parameter.ParameterId} {parameter.Title} {parameter.DisplayValue}");
//    }
//}

////var systemInfo = await httpClient.GetStringAsync("https://api.nibeuplink.com/api/v1/systems/158373/units/");
//// systemid 158373

//Console.ReadLine();

//var scanner = new TuyaScanner();
//scanner.OnNewDeviceInfoReceived += Scanner_OnNewDeviceInfoReceived;
//Console.WriteLine("Scanning local network for Tuya devices, press any key to stop.");
//scanner.Start();
//Console.ReadKey();
//scanner.Stop();

//static void Scanner_OnNewDeviceInfoReceived(object? sender, TuyaDeviceScanInfo e)
//{
//    Console.WriteLine($"New device found! IP: {e.IP}, ID: {e.GwId}, version: {e.Version}");
//}

//var api = new TuyaApi(region: TuyaApi.Region.CentralEurope, accessId: "fwtpvgrn3f7cew7jgs8v", apiSecret: "f1b5b1d4e8124e14bf9e628a7339b873");
//var devices = await api.GetAllDevicesInfoAsync(anyDeviceId: "107073648cce4edda150");
//foreach (var device in devices)
//{
//    Console.WriteLine($"device: {device.Name}, device id: {device.Id}, local key: {device.LocalKey}");
//}

//return;

//var deviceAddresses = new[]
//{
//    ("Kabinet",
//    new DeviceAddress
//    {
//        IP = "tew-752dru",
//        Port = 6666,
//        DeviceId = "107073648cce4eddf8a4",
//        LocalKey = "e8321b5c4e54003b"
//    }),
//    ("Magamistuba",
//    new DeviceAddress
//    {
//        IP = "tew-752dru",
//        Port = 6670,
//        DeviceId = "1070736424a16038cd04",
//        LocalKey = "405c93048404b2bc"
//    }),
//    ("I k pesuruum",
//    new DeviceAddress
//    {
//        IP = "tew-752dru",
//        Port = 6669,
//        DeviceId = "107073648cce4ede49bf",
//        LocalKey = "3384a4b10c15d635"
//    }),
//    ("Elutuba",
//    new DeviceAddress
//    {
//        IP = "tew-752dru",
//        Port = 6668,
//        DeviceId = "107073648cce4edda150",
//        LocalKey = "aea7ea5709cebf54"
//    }),
//    ("II k pesuruum",
//    new DeviceAddress
//    {
//        IP = "tew-752dru",
//        Port = 6667,
//        DeviceId = "1070736424a16038cd93",
//        LocalKey = "ccbebeaf9fbc6892"
//    }),
//    ("Kassituba",
//    new DeviceAddress
//    {
//        IP = "tew-752dru",
//        Port = 6665,
//        DeviceId = "107073648cce4edddfde",
//        LocalKey = "6fccb15132fa3eaa"
//    })
//};

//foreach ((var deviceName, var deviceAddress) in deviceAddresses)
//{
//    var device = new Device
//    {
//        Name = deviceName,
//        Address = JsonSerializer.Serialize(deviceAddress)
//    };

//    device.DevicePoints.Add(new DevicePoint
//    {
//        DataTypeId = 1,
//        Name = "Temperature setpoint",
//        Address = "2"
//    });
//    device.DevicePoints.Add(new DevicePoint
//    {
//        DataTypeId = 1,
//        Name = "Temperature",
//        Address = "3"
//    });

//    if (deviceName.Contains("pesuruum"))
//        device.DevicePoints.Add(new DevicePoint
//        {
//            DataTypeId = 1,
//            Name = "Floor temperature",
//            Address = "102"
//        });

//    dbContext.Devices.Add(device);
//}

//dbContext.SaveChanges();

//return;

//int i = 0;
//while (i < deviceAddresses.Length)
//{
//    (var deviceName, var deviceAddress) = deviceAddresses[i];
//    try
//    {
//        TuyaLocalResponse response;
//        //var device = new TuyaDevice(ip: "tew-752dru", port: 6666, localKey: "3384a4b10c15d635", deviceId: "107073648cce4ede49bf", protocolVersion: TuyaProtocolVersion.V33);
//        //using (var device = new TuyaDevice(ip: "tew-752dru", port: 6668, localKey: "aea7ea5709cebf54", deviceId: "107073648cce4edda150", protocolVersion: TuyaProtocolVersion.V33))
//        //using (var device = new TuyaDevice(ip: "tew-752dru", port: 6666, localKey: "405477382766503b", deviceId: "107073648cce4eddf8a4", protocolVersion: TuyaProtocolVersion.V33, receiveTimeout: 200))
//        using (var device = new TuyaDevice(deviceAddress.IP, deviceAddress.LocalKey, deviceAddress.DeviceId, TuyaProtocolVersion.V33, deviceAddress.Port!.Value, 500))
//        //using (var device = new TuyaDevice(ip: "tew-752dru", port: 6667, localKey: "aea7ea5709cebf54", deviceId: "107073648cce4edda150", protocolVersion: TuyaProtocolVersion.V33, receiveTimeout: 5000))
//        {
//            var dps = await device.GetDpsAsync();

//            Console.WriteLine(deviceName);
//            foreach ((var id, var value) in dps)
//            {
//                Console.WriteLine($"{id}: {value}::{value?.GetType().Name}");
//            }
//            Console.WriteLine();

//            ////Console.WriteLine(string.Join("\n", dps));

//            ////byte[] request = device.EncodeRequest(TuyaCommand.CONTROL, device.FillJson("{\"dps\":{\"" + i + "\":null}}"));
//            //byte[] request = device.EncodeRequest(TuyaCommand.DP_QUERY, device.FillJson("{\"dps\":{\"" + i + "\":null}}"));
//            //byte[] encryptedResponse = await device.SendAsync(request);
//            ////encryptedResponse = StringToByteArrayFastest("42416f68626d64366147393149465231c052e611d4e90d3f044ecfeb0aa17ce2c9653977c60de4ed576ff7b8e503d9d82024ef6fb81c08c2a5208db5a1c2eb28b78321348815bd1cad65d8708f1cf228fb79d058e2f1db1f2818d065d6fd99469e5607fbf2b31494904fe8517518c45f6cde1a2cab10d3f585e95797f1ac644e91a920d54c8f38e40e659385888b93dfd2a6e732b1ededb2bfad09fe679a0f810e07c6f9226860af0b9595709234b65ef2eed3448f9ca5014c19ce835b3167b801580a9c19ae3e306e3a66f508128fd4");
//            ////Console.WriteLine(BitConverter.ToString(encryptedResponse));

//            ////File.WriteAllBytes("response.bin", encryptedResponse);

//            //response = device.DecodeResponse(encryptedResponse);
//        }

//        //var result = TestParser.DecodeResponse(encryptedResponse, Encoding.UTF8.GetBytes(device.LocalKey));

//        //Console.WriteLine($"Response JSON: {response.JSON}");
//    }
//    catch (Exception)
//    {
//        Console.WriteLine($"Reading dp {i} failed");
//    }
//    i++;

//    //var l = Console.ReadLine();
//    //if (l == "x")
//    //    return;
//}

static byte[] StringToByteArrayFastest(string hex)
{
    if (hex.Length % 2 == 1)
        throw new Exception("The binary key cannot have an odd number of digits");

    byte[] arr = new byte[hex.Length >> 1];

    for (int i = 0; i < hex.Length >> 1; ++i)
    {
        arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
    }

    return arr;
}

static int GetHexVal(char hex)
{
    int val = (int)hex;
    //For uppercase A-F letters:
    //return val - (val < 58 ? 48 : 55);
    //For lowercase a-f letters:
    //return val - (val < 58 ? 48 : 87);
    //Or the two combined, but a bit slower:
    return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
}

//var api = new TuyaApi(region: TuyaApi.Region.CentralEurope, accessId: "fwtpvgrn3f7cew7jgs8v", apiSecret: "f1b5b1d4e8124e14bf9e628a7339b873");
////api.GetDeviceInfoAsync
//var devspec = await api.RequestAsync(TuyaApi.Method.GET, "/v1.0/iot-03/devices/protocol?device_ids=107073648cce4eddf8a4");

//Console.ReadLine();

//var bytes = Convert.FromBase64String("BQYsAAgsHgsuHg0uABMoABYoAAgsAAosHgssHg0uABMoABYoAAgsAAosHgssHg0uABMoABYo");
//var text = System.Text.Encoding.UTF8.GetString(bytes);

//Console.ReadLine();


using var httpClient = new HttpClient();

//var content = new FormUrlEncodedContent([
//    new KeyValuePair<string, string>("client_id", "6d34a8a5-0c56-4f0e-a492-b8b167977b80"),
//    new KeyValuePair<string, string>("client_secret", "8BACD656E79135A7A39980A1E074E77C"),
//    new KeyValuePair<string, string>("response_type", "code"),
//    new KeyValuePair<string, string>("grant_type", "client_credentials"),
//    new KeyValuePair<string, string>("scope", "READSYSTEM WRITESYSTEM"),
//    new KeyValuePair<string, string>("redirect_uri", "https://test.com"),
//    ]);



//var response = await httpClient.PostAsync("https://api.myuplink.com/oauth/token", content);

//var responseText = await response.Content.ReadAsStringAsync();

//var tokenInfo = JsonSerializer.Deserialize<AccessTokenInfo>(responseText);

//if (tokenInfo?.AccessToken is null)
//    return;

//var tokenProvider = new AccessTokenProvider(tokenInfo.AccessToken);
//var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
//// Create request adapter using the HttpClient-based implementation
//var adapter = new HttpClientRequestAdapter(authProvider);
//adapter.BaseUrl = "https://api.myuplink.com/";
//// Create the API client
//var client = new MyUplinkClient(adapter);
var client = await MyUplinkClientFactory.Create(httpClient, "6d34a8a5-0c56-4f0e-a492-b8b167977b80", "8BACD656E79135A7A39980A1E074E77C");
if (client == null)
    return;

var systems = await client.V2.Systems.Me.GetAsync();
var deviceId = systems?.Systems?.FirstOrDefault()?.Devices?.FirstOrDefault()?.Id;
if (deviceId == null)
    return;

//var heatPumpDevice = new Device
//{
//    Name = "Heat pump",
//    Address = JsonSerializer.Serialize(new DeviceAddress
//    {
//        DeviceId = deviceId,
//        ClientId = "6d34a8a5-0c56-4f0e-a492-b8b167977b80",
//        ClientSecret = "8BACD656E79135A7A39980A1E074E77C"
//    }, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
//    IsEnabled = true,
//    Type = "heat_pump"
//};
//dbContext.Devices.Add(heatPumpDevice);
//dbContext.SaveChanges();

var heatPumpDevice = dbContext.Devices.FirstOrDefault(x => x.Type == "heat_pump");
//heatPumpDevice.Address = JsonSerializer.Serialize(new DeviceAddress
//{
//    DeviceId = deviceId,
//    ClientId = "6d34a8a5-0c56-4f0e-a492-b8b167977b80",
//    ClientSecret = "8BACD656E79135A7A39980A1E074E77C"
//}, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
//dbContext.SaveChanges();

var units = dbContext.Units.ToDictionary(x => x.Name);
var dataTypes = dbContext.DataTypes.ToDictionary(x => x.Name);
var floatDataType = dataTypes["Float"];

var points = await client.V2.Devices[deviceId].Points.GetAsync();

foreach (var point in points!)
{
    int.TryParse(point.ParameterId, out var parameterId);

    //if (point.ParameterUnit is "°C" or "bar" or "Hz" or "A" && parameterId <= 44701 && parameterId > 0)
    if (point.ParameterUnit is "%" && parameterId > 0)
    {
        var name = point.ParameterName ?? "";
        for (int i = 1; i < name.Length - 1; i++)
        {
            var cp = name[i - 1];
            var c = name[i];
            var cn = name[i + 1];
            if ((c == '-' || c == (char)173) && char.IsLetter(cp) && char.IsLetter(cn))
                name = name[..i] + name[(i + 1)..];
        }

        var rawValue = point.Value;
        string value = rawValue?.ToString() ?? "";
        if (rawValue is UntypedDecimal untypedDecimal)
            value = untypedDecimal.GetValue().ToString();
        Console.WriteLine($"{point.ParameterId} {name,-40} {point.StrVal,-10} {value,-10} {point.ParameterUnit} {string.Join(", ", point.EnumValues!.Select(x => x.Text + " " + x.Value))}");

        var devicePoint = new DevicePoint
        {
            Device = heatPumpDevice!,
            Name = name,
            Address = parameterId.ToString(),
            DataType = floatDataType,
            Unit = units[point.ParameterUnit],
        };
        dbContext.DevicePoints.Add(devicePoint);
    }
}

dbContext.SaveChanges();

Console.ReadLine();


//Console.WriteLine(responseText);

//using var httpClient = new HttpClient();
//httpClient.Timeout = TimeSpan.FromMinutes(10);

////var content = new StringContent("{\"id\":1,\"method\":\"HTTP.GET\",\"params\":{\"url\":\"http://10.33.53.21/rpc/Shelly.GetDeviceInfo\"}}' http://${SHELLY}/rpc");

////var response = await httpClient.PostAsync("http://192.168.1.130:6700/rpc", content);

//var response = await httpClient.GetAsync("http://192.168.1.130:6701/rpc/Shelly.GetStatus");
////var response = await httpClient.GetAsync("http://192.168.1.130:6700/rpc/Light.Set?id=0&brightness=27.09");

//var responseText = await response.Content.ReadAsStringAsync();

//using var jDoc = JsonDocument.Parse(responseText);
//responseText = JsonSerializer.Serialize(jDoc, new JsonSerializerOptions { WriteIndented = true });

//Console.WriteLine(responseText);
