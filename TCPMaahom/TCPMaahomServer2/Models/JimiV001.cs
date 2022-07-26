using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPMaahomServer2
{
    class JimiV001
    {
        static Dictionary<string, JimiDevice> tokens = new Dictionary<string, JimiDevice>();

        public static byte[] ParseDate(byte[] data)
        {

            int index = 1;

            byte[] messageID = new byte[2];
            Array.Copy(data, index, messageID, 0, 2);
            index += 2;

            byte[] message_body_prob = new byte[2];
            Array.Copy(data, index, message_body_prob, 0, 2);
            index += 2;

            byte[] sn = new byte[6];
            Array.Copy(data, index, sn, 0, 6);
            index += 6;

            byte[] message_sequence = new byte[2];
            Array.Copy(data, index, message_sequence, 0, 2);
            index += 2;

            byte[] pocket_massage = new byte[2];

            var has_pocket_massage = (message_body_prob[0] & (1 << 6 - 1)) != 0;

            if(has_pocket_massage)
            {
                Array.Copy(data, index, pocket_massage, 0, 2);
                index += 2;
            }

            var body_int = (message_body_prob[0] << 8) | message_body_prob[1];
            var body_length = 0x1FF & body_int;

            byte[] body = new byte[body_length];
            Array.Copy(data, index, body, 0, body_length);
            index += body_length;

            var check = data[index];

            byte[] counter = new byte[2];
            var str_sn = BitConverter.ToString(sn).Replace("-", "");
            if (tokens.ContainsKey(str_sn))
            {
                tokens[str_sn].IncreaseCounter();
                counter = tokens[str_sn].GetCounterByte();
            }

            if (messageID[0] == 0x00 && messageID[1] == 0x02)
            {
                ParseHeartBeat(sn);
            }
            else if(messageID[0] == 0x02 && messageID[1] == 0x00)
            {
                ParseLocation(body,sn);
            }
            else if (messageID[0] == 0x09 && messageID[1] == 0x00)
            {
                ParseReading(body, sn);
            }
            else if (messageID[0] == 0x01 && messageID[1] == 0x00)
            {
                JimiDevice dev = new JimiDevice();

                int index2 = 0;

                Array.Copy(body, index2, dev.Devicemanufacturer, 0, 2);
                index2 += 2;

                Array.Copy(body, index2, dev.Authenticationlevel, 0, 2);
                index2 += 2;

                Array.Copy(body, index2, dev.DeviceType, 0, 5);
                index2 += 5;

                Array.Copy(body, index2, dev.SIM, 0, 20);
                index2 += 20;

                Array.Copy(body, index2, dev.TerminalSN, 0, 7);
                index2 += 7;

                dev.LicensePlateColor = body[index2];
                index2 += 1;

                //var len = body.Length - index2 - 1;
                //byte[] tarray = new byte[len];

                //Array.Copy(body, index2, tarray, 0, len);
                //index2 += len;

                if (tokens.ContainsKey(str_sn))
                {
                    tokens[str_sn] = dev;
                }
                else
                    tokens.Add(str_sn, dev);

                var response = RegisterResponseMessage(sn, message_sequence);

                return response.ToArray();
            }


            var others = GeneralResponseMessage(0x00,sn, message_sequence, messageID, counter);

            return others.ToArray();
        }

        private static async Task ParseReading(byte[] body, byte[] SN)
        {
            int index2 = 0;

            var TransparentMessageType = body[index2];
            index2 += 1;

            if (TransparentMessageType == 0xF0)
            {
                byte[] time = new byte[6];
                Array.Copy(body, index2, time, 0, 6);
                index2 += 6;

                index2 += 1;
                index2 += 1;

                var MessageSubcategory = body[index2];
                index2 += 1;

                if (MessageSubcategory == 0x01)
                {
                    var countItems = body[index2];
                    index2 += 1;

                    List<ReadingList> items = new List<ReadingList>();

                    for (int i = 0; i < countItems; i++)
                    {
                        byte[] DataID = new byte[2];
                        Array.Copy(body, index2, DataID, 0, 2);
                        index2 += 2;

                        var DAtaIDInt = ConvertDWORDToint16(DataID);

                        var LengthofData = body[index2];
                        index2 += 1;

                        List<byte> ValueOfDate = new List<byte>();

                        for (int j = 0; j < LengthofData; j++)
                        {
                            ValueOfDate.Add(body[index2]);
                            index2++;
                        }

                        int value = 0;

                        if (ValueOfDate.Count > 1)
                        {
                            ValueOfDate.Reverse();

                            if (ValueOfDate.Count == 2)
                            {
                                value = BitConverter.ToInt16(ValueOfDate.ToArray(), 0);
                            }
                            else if (ValueOfDate.Count == 4)
                            {
                                value = BitConverter.ToInt32(ValueOfDate.ToArray(), 0);
                            }

                        }
                        else
                        {
                            value = ValueOfDate[0];
                        }

                        var itemName = "";
                        var isDefined = Enum.IsDefined(typeof(DataStream), DAtaIDInt);

                        if (isDefined)
                        {
                            itemName = ((DataStream)DAtaIDInt).ToString();
                        }

                        ReadingList l = new ReadingList
                        {
                            Name = itemName,
                            HexValue = DAtaIDInt.ToString(),
                            Value = value,
                        };
                        items.Add(l);
                    }

                    if (items.Any())
                    {
                        SendReading(items, time, SN);
                    }
                }
                else if (MessageSubcategory == 0x03)
                {
                    var countItems = body[index2];
                    index2 += 1;

                    List<String> items = new List<string>();

                    for (int i = 0; i < countItems; i++)
                    {
                        byte DataID = body[index2];
                        index2 += 1;

                        var LengthofData = body[index2];
                        index2 += 1;

                        for (int j = 0; j < LengthofData; j++)
                        {
                            index2++;
                        }

                        var itemName = "";
                        var isDefined = Enum.IsDefined(typeof(ErrorBehavior), (int)DataID);

                        if (isDefined)
                        {
                            itemName = ((ErrorBehavior)DataID).ToString();
                            items.Add(itemName);
                        }
                    }

                    if (items.Any())
                    {
                        SendEvent(items, time, SN);
                    }
                }
            }
        }

        private static async Task SendReading(List<ReadingList> items, byte[] time, byte[] SN)
        {
            var sn = ConvertSNToLong(SN);
            var timed = GetTime(time);

            foreach (var item in items)
            {
                await ServerHelper.SendReading(sn.ToString(), item.Name, item.HexValue, item.Value.ToString(), timed.ToString());
            }
        }

        private static async Task SendEvent(List<string> items, byte[] time, byte[] SN)
        {
            var sn = ConvertSNToLong(SN);
            var timed = GetTime(time);

            foreach (var item in items)
            {
                await ServerHelper.SendEvent(sn.ToString(), item, timed.ToString());
            }
        }

        private static async Task ParseLocation(byte[] body,byte[] SN)
        {
            int index = 0;

            byte[] Alarmflag = new byte[4];
            Array.Copy(body, index, Alarmflag, 0, 4);
            index += 4;

            byte[] Status = new byte[4];
            Array.Copy(body, index, Status, 0, 4);
            index += 4;

            byte[] Latitude = new byte[4];
            Array.Copy(body, index, Latitude, 0, 4);
            index += 4;

            byte[] Longitude = new byte[4];
            Array.Copy(body, index, Longitude, 0, 4);
            index += 4;

            byte[] Elevation = new byte[2];
            Array.Copy(body, index, Elevation, 0, 2);
            index += 2;

            byte[] Speed = new byte[2];
            Array.Copy(body, index, Speed, 0, 2);
            index += 2;

            byte[] Direction = new byte[2];
            Array.Copy(body, index, Direction, 0, 2);
            index += 2;

            byte[] Time = new byte[6];
            Array.Copy(body, index, Time, 0, 6);
            index += 6;

            var lat = ConvertDWORDToint32(Latitude) / 1000000.0;
            var lng = ConvertDWORDToint32(Longitude) / 1000000.0;
            var speed = ConvertDWORDToint16(Speed) / 10.0;
            var sn = ConvertSNToLong(SN);

            var timed = GetTime(Time);

            await ServerHelper.SendData(sn.ToString(), lat.ToString(), lng.ToString(), speed.ToString(), timed.ToString());
        }

        private static async Task ParseHeartBeat(byte[] SN)
        {
            var sn = ConvertSNToLong(SN);

            await ServerHelper.SendHeartBeat(sn.ToString());
        }

        private static long GetTime(byte[] Time)
        {
            var year = int.Parse(Time[0].ToString("X")) + 2000;
            var month = int.Parse(Time[1].ToString("X"));
            var day = int.Parse(Time[2].ToString("X"));
            var hour = int.Parse(Time[3].ToString("X"));
            var minute = int.Parse(Time[4].ToString("X"));
            var seconds = int.Parse(Time[5].ToString("X"));

            var time = new DateTime(year, month, day, hour, minute, seconds);
            var timed = time.ToBinary();

            return timed;
        }
       

        private static List<byte> GeneralResponseMessage(byte results,byte[] sn, byte[] message_sequence, byte[] responseID, byte[] counter)
        {
            List<byte> header = new List<byte>();

            header.Add(0x80);
            header.Add(0x01);

            header.Add(0x00);
            header.Add(0x05);

            header.Add(sn[0]);
            header.Add(sn[1]);
            header.Add(sn[2]);
            header.Add(sn[3]);
            header.Add(sn[4]);
            header.Add(sn[5]);

            header.Add(counter[0]);
            header.Add(counter[1]);

            List<byte> body = new List<byte>();

            body.Add(message_sequence[0]);
            body.Add(message_sequence[1]);

            body.Add(responseID[0]);
            body.Add(responseID[1]);

            body.Add(results);

            List<byte> header_body = new List<byte>();

            header_body.AddRange(header);
            header_body.AddRange(body);

            var check = XOR(header_body.ToArray());

            List<byte> response = new List<byte>();

            response.Add(0x7e);
            response.AddRange(header_body);
            response.Add(check);
            response.Add(0x7e);

            return response;
        }

        private static List<byte> RegisterResponseMessage(byte[] sn, byte[] message_sequence)
        {
            List<byte> header = new List<byte>();

            header.Add(0x81);
            header.Add(0x00);

            header.Add(0x00);
            header.Add(0x23);

            header.Add(sn[0]);
            header.Add(sn[1]);
            header.Add(sn[2]);
            header.Add(sn[3]);
            header.Add(sn[4]);
            header.Add(sn[5]);

            header.Add(0x00);
            header.Add(0x01);

            List<byte> body = new List<byte>();

            body.Add(message_sequence[0]);
            body.Add(message_sequence[1]);

            body.Add(0);

            var skey = RandomString();
            var skeyb = System.Text.Encoding.UTF8.GetBytes(skey);

            for (int i = 0; i < skeyb.Length;i++)
            {
                var s = skeyb[i];
                if (s == 0x7e || s == 0x7d)
                   s = 0x82;

                body.Add(s);
            }

            List<byte> header_body = new List<byte>();

            header_body.AddRange(header);
            header_body.AddRange(body);

            var check = XOR(header_body.ToArray());

            List<byte> response = new List<byte>();

            response.Add(0x7e);
            response.AddRange(header_body);
            response.Add(check);
            response.Add(0x7e);

            return response;
        }


        private static byte XOR(byte[] data)
        {
            if (data.Length > 1)
            {
                byte xor_byte = (byte)(data[0] ^ data[1]);

                for (int i = 2; i < data.Length; i++)
                {
                    xor_byte = (byte)(xor_byte ^ data[i]);
                }

                return xor_byte;
            }
            else
                return 0;
        }

        private static String RandomString()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[32];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);

            return finalString;
        }

        static int ConvertDWORDToint16(byte[] dword)
        {
            byte[] value = new byte[2];
            Array.Copy(dword, value, 2);
            Array.Reverse(value); // otherwise you will have a1612240fff result
            var result = (int)BitConverter.ToInt16(value, 0);
            return result;
        }

        static int ConvertDWORDToint32(byte[] dword)
        {
            byte[] value = new byte[4];
            Array.Copy(dword, value, 4);
            Array.Reverse(value); // otherwise you will have a1612240fff result
            var result = BitConverter.ToInt32(value, 0);
            return result;
        }

        static ulong ConvertSNToLong(byte[] sn)
        {
            byte[] value = new byte[8];
            Array.Copy(sn,0, value,2, 6);
            Array.Reverse(value); // otherwise you will have a1612240fff result
            var result = BitConverter.ToUInt64(value, 0);
            return result;
        }


    }

    class JimiDevice
    {
        public byte[] Devicemanufacturer = new byte[2];
        public byte[] Authenticationlevel = new byte[2];
        public byte[] DeviceType = new byte[5];
        public byte[] SIM = new byte[20];
        public byte[] TerminalSN = new byte[7];
        public byte LicensePlateColor = 0;
        public string VehicleIdentification = "";
        public ushort counter = 1;

        public void IncreaseCounter()
        {
            if (counter < 65534)
            {
                counter = 1;
            }
            else
                counter = 0;
        }

        public byte[] GetCounterByte()
        {
            var bytes = BitConverter.GetBytes(counter);
            if(BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
    }

    public class ReadingList
    {
        public string Name;
        public string HexValue;
        public int Value;
    }

    public enum DataStream
    {
        TotalVehicleMileage = 0x0528,
        TotalVehicleFuel = 0x052C,
        VehicleMileage = 0x0102,
        VehicleFuel = 0x0103,
        AccumulatedMileage = 0x0546,
        CumulativeFuelConsumption = 0x0105,
        Voltage = 0x0530,
    }

    public enum ErrorBehavior
    {
        Self_Check = 0x00,
        Plug_In = 0x01,
        Plug_Out = 0x02,
        High_Voltage = 0x03,
        Low_Voltage = 0x04,
        High_Water_Temperature = 0x05,
        Low_Water_Temperature = 0x06,
        High_Oil_Temperature = 0x07,
        High_Fuel_Temperature = 0x08,
        High_Oil_Pressure = 0x09,
        Tire_Pressure = 0x0A,
        Low_Fuel = 0x0B,
        Feed_Reminder = 0x0C,
        Long_Preheadting_Duration = 0x0D,
        Overrun_Idling_Time = 0x0E,
        Driving_Low_Fuel = 0x0F,
        Cold_Started_High_Speed = 0x10,
        Driving_Night_No_Lights = 0x11,
        Driving_Handbrake_Not_Released = 0x12,
        Driving_Doors_Open = 0x13,
        Driving_Doors_Unlocked = 0x14,
        Driving_Trunk_Open = 0x15,
        Coasting_Neutral = 0x16,
        Driver_Not_Buckledup = 0x17,
        Passenger_Not_Backledup = 0x18,
        Quick_Fuelup = 0x19,
        Sudden_Acceleration = 0x1A,
        Hard_Braking = 0x1B,
        Shap_Turn = 0x1C,
        Rapid_Lane_Change = 0x1D,
        Crossing_Multiple_Lanes_At_Once = 0x1E,
        Continuous_Lane_Change = 0x1F,
        Emergency = 0x20,
        Left_Geofence = 0x21,
        Entred_Geofence = 0x22,
        Fatigue_Driving = 0x23,
        Cumulative_Driving_Duration = 0x24,
        Speed_Driving = 0x25,
        Ordinary_Vehicle_Collision = 0x26,
        Severe_Vehicle_Collision = 0x27,
        Vehicle_Rollover = 0x28,
        Break_Down_For_Longtime = 0x29,
        Clutch_Down_For_Longtime = 0x2A,
        Riding_The_Clutch = 0x2B,
        Gear = 0x2C,
        Parking_Without_PNGear = 0x2D,
        Collision_Parking = 0x2E,
        Fuel_Theft = 0x2F,
        Tow = 0x30,
        Doors_Open = 0x31,
        Doors_Unlock = 0x32,
        Window_Open = 0x33,
        Trunk_Open = 0x34,
        Sunroof_Open = 0x35,
        Fuel_Cap_Open = 0x36,
        Lights_Left_On = 0x37,
        Ignition_On = 0x38,
        IGnition_Off = 0x39,
        Wake_Up = 0x3A,
        Abnormal_Urea = 0x3B,
        Increase_Urea = 0x3C,
        Malfunction = 0x3D,
        Abnormal_Fuel_Level = 0x3E,
    }
}
