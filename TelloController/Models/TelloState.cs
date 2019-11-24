using System.Linq;

namespace TelloController.Models
{
    public class TelloState
    {
        private TelloState()
        {
        }

        public static TelloState Parse(int timestamp, string rawState)
        {
            var state = new TelloState();

            var formattedRawState = rawState.TrimEnd('\n').TrimEnd('\r').TrimEnd(';');

            state.Timestamp = timestamp;
            state.RawState = rawState;

            var entries = formattedRawState.Split(';');
            var values = entries.Select(entry => entry.Split(':')[1]).ToList();

            state.Mid = int.Parse(values[0]);
            state.X = double.Parse(values[1]);
            state.Y = double.Parse(values[2]);
            state.Z = double.Parse(values[3]);
            state.Mpry = values[4];
            state.Pitch = double.Parse(values[5]);
            state.Roll = double.Parse(values[6]);
            state.Yaw = double.Parse(values[7]);
            state.SpeedX = double.Parse(values[8]);
            state.SpeedY = double.Parse(values[9]);
            state.SpeedZ = double.Parse(values[10]);
            state.TemperatureLow = int.Parse(values[11]);
            state.TemperatureHigh = int.Parse(values[12]);
            state.Tof = int.Parse(values[13]);
            state.Height = int.Parse(values[14]);
            state.BatteryPercentage = int.Parse(values[15]);
            state.Barometer = double.Parse(values[16]);
            state.MotorsOnTime = int.Parse(values[17]);
            state.AccelX = double.Parse(values[18]);
            state.AccelY = double.Parse(values[19]);
            state.AccelZ = double.Parse(values[20]);

            return state;
        }

        public int Timestamp { get; private set; }
        public string RawState { get; private set; }       
        public int Mid { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }
        public string Mpry { get; private set; }
        public double Pitch { get; private set; }
        public double Roll { get; private set; }
        public double Yaw { get; private set; }
        public double SpeedX { get; private set; }
        public double SpeedY { get; private set; }
        public double SpeedZ { get; private set; }
        public int TemperatureLow { get; private set; }
        public int TemperatureHigh { get; private set; }
        public int Tof { get; private set; }
        public int Height { get; private set; }
        public int BatteryPercentage { get; private set; }
        public double Barometer { get; private set; }
        public int MotorsOnTime { get; private set; }
        public double AccelX { get; private set; }
        public double AccelY { get; private set; }
        public double AccelZ { get; private set; }

        public static string GetCsvHeaderRow()
        {
            return "Timestamp,Pitch,Roll,Yaw,SpeedX,SpeedY,SpeedZ,TempLow,TempHigh,Tof,Height,Battery,Baro,MotorsOnTime,AccelX,AccelY,AccelZ";
        }

        public string GetCsvDataRow()
        {
            return 
                $"{Timestamp},{Pitch},{Roll},{Yaw},{SpeedX},{SpeedY},{SpeedZ}," +
                $"{TemperatureLow},{TemperatureHigh},{Tof},{Height},{BatteryPercentage}," +
                $"{Barometer},{MotorsOnTime},{AccelX},{AccelY},{AccelZ}";
        }

        public override string ToString()
        {
            return $"Timestamp: {Timestamp} | Bat: {BatteryPercentage} | Pitch: {Pitch} Roll: {Roll} Yaw: {Yaw} | AccelX: {AccelX} AccelY: {AccelY} AccelZ: {AccelZ}";
        }
    }
}
