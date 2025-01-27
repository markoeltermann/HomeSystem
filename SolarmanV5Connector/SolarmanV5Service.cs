using System.Net;
using System.Net.Sockets;

namespace SolarmanV5Connector;

public class SolarmanV5Service(ILogger<SolarmanV5Service> logger, IConfiguration configuration)
{
    private readonly object syncRoot = new();
    private readonly byte[] buffer = new byte[1024];

    public List<PointValueDto>? ReadValues(IList<int> addresses)
    {
        if (addresses == null)
            return null;

        if (GetSettings() is not var (loggerIP, loggerPort, loggerSerial, modbusId))
        {
            return null;
        }

        if (!addresses.Any())
            return [];

        if (addresses.Any(x => x is < 0 or > 0xffff))
        {
            throw new BadRequestException("Address cannot be less than 0 or more than 0xffff.");
        }

        var addressBlocks = addresses.Select(x => (ushort)x).OrderBy(x => x).GroupBy(x => x / 100).Select(x => x.ToArray());

        var result = new List<PointValueDto>();

        foreach (var addressBlock in addressBlocks)
        {
            var minAddress = addressBlock.Min();
            var maxAddress = addressBlock.Max();

            var values = ReadValueBlock(minAddress, (ushort)(maxAddress - minAddress + 1), loggerSerial, modbusId, loggerIP, loggerPort);
            if (values == null)
            {
                return null;
            }

            foreach (var address in addressBlock)
            {
                result.Add(new PointValueDto(address, values[address - minAddress]));
            }
        }

        return result;
    }

    public bool WriteValue(int address, int value)
    {
        if (GetSettings() is not var (loggerIP, loggerPort, loggerSerial, modbusId))
        {
            return false;
        }

        if (address < 0 || address > 0xffff || value < 0 || value > 0xffff)
        {
            throw new BadRequestException("Address and value must be between 0 and 0xffff");
        }

        var modbusFrame = GetWriteHoldingRegisterFrame(modbusId, (ushort)address, (ushort)value);

        var responseModbusFrame = SendModbusFrame(modbusFrame, loggerSerial, loggerIP, loggerPort);
        if (responseModbusFrame == null || responseModbusFrame.Length != 8)
        {
            return false;
        }

        for (int i = 0; i < responseModbusFrame.Length - 2; i++)
        {
            if (modbusFrame[i] != responseModbusFrame[i])
                return false;
        }

        return true;
    }

    private ushort[]? ReadValueBlock(ushort registerAddress, ushort registerCount, uint loggerSerial, byte modbusId, IPAddress loggerIP, int loggerPort)
    {
        lock (syncRoot)
        {
            var modbusFrame = GetReadHoldingRegistersFrame(modbusId, registerAddress, registerCount);

            modbusFrame = SendModbusFrame(modbusFrame, loggerSerial, loggerIP, loggerPort);
            if (modbusFrame == null)
            {
                return null;
            }

            var byteCount = modbusFrame[2];
            if (byteCount != registerCount * 2)
            {
                logger.LogError("Invalid Modbus response received - value byte count is not correct.");
                return null;
            }

            var values = new ushort[registerCount];

            for (int i = 0; i < registerCount; i++)
            {
                var high = modbusFrame[i * 2 + 3];
                var low = modbusFrame[i * 2 + 4];
                var value = (high << 8) + low;

                values[i] = (ushort)value;
            }

            return values;
        }
    }

    private byte[]? SendModbusFrame(byte[] modbusFrame, uint loggerSerial, IPAddress loggerIP, int loggerPort)
    {
        var frame = GetOutgoingFrame(loggerSerial, modbusFrame);

        var endpoint = new IPEndPoint(loggerIP, loggerPort);
        using var tcpClient = new TcpClient();
        tcpClient.Connect(endpoint);
        using var tcpStream = tcpClient.GetStream();
        tcpStream.Write(frame);

        var r = tcpStream.Read(buffer, 0, 1024);
        if (r >= 1024)
        {
            return null;
        }

        var response = buffer.Take(r).ToArray();
        var payloadLength = response[1] + response[2] * 0x100;
        modbusFrame = response.Skip(11).Take(payloadLength).Skip(14).ToArray();

        if (modbusFrame.Length < 7)
        {
            logger.LogError("Could not get a valid Modbus frame.");
            return null;
        }

        var crc = CalculateModbusCRC(modbusFrame);

        if (modbusFrame[^2] != (byte)crc || modbusFrame[^1] != (byte)(crc >> 8))
        {
            logger.LogError("Modbus frame CRC was incorrect.");
            return null;
        }

        return modbusFrame;
    }

    private static byte[] GetOutgoingFrame(uint loggerSerial, byte[] modbusFrame)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Header
        bw.Write((byte)0xa5);
        bw.Write((ushort)0);
        bw.Write((ushort)0x4510);
        bw.Write((ushort)0x9900);
        bw.Write(loggerSerial);

        // Payload
        bw.Write((byte)0x02);
        bw.Write((ushort)0);
        bw.Write((uint)0);
        bw.Write((uint)0);
        bw.Write((uint)0);

        bw.Write(modbusFrame);

        // Trailer
        bw.Write((byte)0);
        bw.Write((byte)0x15);

        var frame = ms.ToArray();
        var payloadLength = modbusFrame.Length + 15;
        frame[1] = (byte)payloadLength;
        frame[2] = (byte)(payloadLength << 8);

        frame[^2] = CalculateV5FrameChecksum(frame);

        return frame;
    }

    private static byte[] GetReadHoldingRegistersFrame(byte modbusId, ushort registerAddress, ushort registerCount)
    {
        byte[] modbusFrame = [modbusId, 0x03, (byte)(registerAddress >> 8), (byte)registerAddress, (byte)(registerCount >> 8), (byte)registerCount, 0x00, 0x00];
        SetModbusFrameCRC(modbusFrame);
        return modbusFrame;
    }

    private static byte[] GetWriteHoldingRegisterFrame(byte modbusId, ushort registerAddress, ushort value)
    {
        byte[] modbusFrame = [modbusId, 0x10, (byte)(registerAddress >> 8), (byte)registerAddress, 0, 1, 2, (byte)(value >> 8), (byte)value, 0x00, 0x00];
        SetModbusFrameCRC(modbusFrame);
        return modbusFrame;
    }

    private static void SetModbusFrameCRC(byte[] modbusFrame)
    {
        var crc = CalculateModbusCRC(modbusFrame);
        modbusFrame[^2] = (byte)crc;
        modbusFrame[^1] = (byte)(crc >> 8);
    }

    private (IPAddress ip, int port, uint loggerSerial, byte modbusId)? GetSettings()
    {
        var loggerIP = configuration["LoggerDeviceIP"];
        var loggerPort = configuration["LoggerDevicePort"];
        var loggerSerial = configuration["LoggerDeviceSerial"];
        var modbusId = configuration["ModbusID"];

        if (string.IsNullOrEmpty(loggerIP) || string.IsNullOrEmpty(loggerPort) || string.IsNullOrEmpty(loggerSerial))
        {
            logger.LogError("Some mandatory config variables are missing.");
            return null;
        }
        else if (int.TryParse(loggerPort, out var p)
            && uint.TryParse(loggerSerial, out var s)
            && IPAddress.TryParse(loggerIP, out var ip)
            && byte.TryParse(modbusId, out var id))
        {
            return (ip, p, s, id);
        }
        else
        {
            logger.LogError("Could not parse some config variables.");
            return null;
        }
    }


    private static ushort CalculateModbusCRC(byte[] frame)
    {
        ushort crc = 0xFFFF;

        for (int pos = 0; pos < frame.Length - 2; pos++)
        {
            crc ^= frame[pos];          // XOR byte into least sig. byte of crc

            for (int i = 8; i != 0; i--)
            {    // Loop over each bit
                if ((crc & 0x0001) != 0)
                {      // If the LSB is set
                    crc >>= 1;                    // Shift right and XOR 0xA001
                    crc ^= 0xA001;
                }
                else                            // Else LSB is not set
                    crc >>= 1;                    // Just shift right
            }
        }

        return crc;
    }

    private static byte CalculateV5FrameChecksum(byte[] frame)
    {
        uint checksum = 0;

        for (int i = 1; i < frame.Length - 2; i++)
        {
            checksum += frame[i];
        }

        return (byte)checksum;
    }
}
