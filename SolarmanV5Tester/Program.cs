
//List<byte> smFrame = [];

//smFrame.Add(0xa5);
//smFrame.Add(0);
//smFrame.Add(0);

using System.Net;
using System.Net.Sockets;

var loggerSerial = 2716193869;

var ms = new MemoryStream();
var bw = new BinaryWriter(ms);

bw.Write((byte)0xa5);
bw.Write((ushort)0);
bw.Write((ushort)0x4510);
bw.Write((ushort)0x9900);
bw.Write(loggerSerial);

var msp = new MemoryStream();
var bwp = new BinaryWriter(msp);

bwp.Write((byte)0x02);
bwp.Write((ushort)0);
bwp.Write((uint)0);
bwp.Write((uint)0);
bwp.Write((uint)0);

byte modbusAddress = 0x01;
ushort registerAddress = 600;
ushort registerCount = 100;

byte[] modbusFrame = [modbusAddress, 0x03, (byte)(registerAddress >> 8), (byte)registerAddress, (byte)(registerCount >> 8), (byte)registerCount, 0x00, 0x00];
var crc = ModRTU_CRC(modbusFrame, 6);
modbusFrame[^2] = (byte)crc;
modbusFrame[^1] = (byte)(crc >> 8);

bwp.Write(modbusFrame);

var payload = msp.ToArray();

bw.Write(payload);
bw.Write((byte)0);
bw.Write((byte)0x15);

var frame = ms.ToArray();
var length = payload.Length;
frame[1] = (byte)length;
frame[2] = (byte)(length << 8);
frame[^2] = CalculateV5FrameChecksum(frame);

var frameHex = frame.Select(x => x.ToString("X")).ToArray();

var endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.232"), 8899);
using var tcpClient = new TcpClient();
tcpClient.Connect(endpoint);
using var tcpStream = tcpClient.GetStream();
tcpStream.Write(frame);

var buffer = new byte[1024];
var r = tcpStream.Read(buffer, 0, 1024);

var response = buffer.Take(r).ToArray();
payload = response.Skip(11).Take(response[1] + response[2] * 0x100).ToArray();
modbusFrame = payload.Skip(14).ToArray();
crc = ModRTU_CRC(modbusFrame, modbusFrame.Length - 2);

if (modbusFrame[^2] == (byte)crc && modbusFrame[^1] == (byte)(crc >> 8))
{
    Console.WriteLine("CRC correct");
}

for (int i = 0; i < modbusFrame[2] / 2; i++)
{
    var high = modbusFrame[i * 2 + 3];
    var low = modbusFrame[i * 2 + 4];
    var value = (high << 8) + low;

    Console.WriteLine($"{i:D3} = {value}");
}


Console.ReadLine();

static ushort ModRTU_CRC(byte[] buf, int len)
{
    ushort crc = 0xFFFF;

    for (int pos = 0; pos < len; pos++)
    {
        crc ^= (ushort)buf[pos];          // XOR byte into least sig. byte of crc

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
    /* Note, this number has low and high bytes swapped, so use it accordingly (or swap bytes - 
    here is a simple example: crc =  ((crc>>8) | (crc<<8); <- simple swap, if you need it */

    return crc;

}

static byte CalculateV5FrameChecksum(byte[] frame)
{

    //checksum = 0
    //for i in range(1, len(frame) - 2, 1):
    //    checksum += frame[i] & 0xFF
    //return int(checksum & 0xFF)

    uint checksum = 0;

    for (int i = 1; i < frame.Length - 2; i++)
    {
        checksum += frame[i];
    }

    return (byte)checksum;
}
