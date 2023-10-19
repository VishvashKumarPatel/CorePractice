using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;

namespace AddRemoveAccessors
{
    internal class Program
    {

        static void CheckArray(int[][] number)
        {


        }
        static void Main(string[] args)
        {
            Console.WriteLine("Press any Key to start device");
            int[,] matrix = new int[4, 3]; // Create a 3x3 matrix
            //CheckArray(aray);

            IDevice device = new Device();
            device.RunDevice();
            Console.ReadKey();
        }




        #region ThermoStat
        public class ThermoStat : IThermoStat
        {
            private readonly IHeatSensor _heatSensor;
            private readonly ICoolingMechanism _coolingMechanism;
            private readonly IDevice _device;

            public ThermoStat(IHeatSensor heatSensor, ICoolingMechanism coolingMechanism, IDevice device)
            {
                _heatSensor = heatSensor;
                _coolingMechanism = coolingMechanism;
                _device = device;
            }
            private void WireUpEventToEventHandlers()
            {
                _heatSensor.TemperatureReachedWarningLevelEventHandler += HeatSensor_TemperatureReachedWarningLevelEventHandler;
                _heatSensor.TemperatureReachedEmergencyLevelEventHandler += HeatSensor_TemperatureReachedEmergencyLevelEventHandler;
                _heatSensor.TemperatureFallsBelowWarningLevelEventHandler += HeatSensor_TemperatureFallsBelowWarningLevelEventHandler;
            }
            public void RunThermoStat()
            {
                Console.WriteLine("Thermostat is running");
                WireUpEventToEventHandlers();
                _heatSensor.RunHeatSensor();
            }

            private void HeatSensor_TemperatureFallsBelowWarningLevelEventHandler(object sender, TemperatureEventArgs e)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Temperature has fallen below warning level {_device.WarningLevelTemperature} {e.Temperature} at {e.CurrentDateTime}");
                _coolingMechanism.Off();
                Console.ResetColor();
            }

            private void HeatSensor_TemperatureReachedEmergencyLevelEventHandler(object sender, TemperatureEventArgs e)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Temperature has reached emergency level {e.Temperature} at {e.CurrentDateTime}");
                _device.HandleEmergency();
                Console.ResetColor();
            }

            private void HeatSensor_TemperatureReachedWarningLevelEventHandler(object sender, TemperatureEventArgs e)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Temperature has reached warning level {e.Temperature} at {e.CurrentDateTime}");
                _coolingMechanism.On();
                Console.ResetColor();
            }
        }     

        public interface IThermoStat
        {
            void RunThermoStat();
        }


        #endregion


        public class Device : IDevice
        {
            const double Warning_Level = 27;
            const double Emergency_Level = 72;

            public double WarningLevelTemperature => Warning_Level;

            public double EmergencyLevelTemperature => Emergency_Level;

            public void HandleEmergency()
            {
                Console.WriteLine();
                Console.WriteLine("Sending Out notification emergency service");
                ShutDownDevice();
                Console.WriteLine();
            }

            private void ShutDownDevice()
            {
                Console.WriteLine();
                Console.WriteLine("Shutting down device");
            }

            public void RunDevice()
            {
                Console.WriteLine();
                Console.WriteLine("Device is running..");

                ICoolingMechanism coolingMechanism = new CoolingMechanism();
                IHeatSensor heatSensor = new    HeatSensor(Warning_Level, Emergency_Level);
                IThermoStat thermoStat = new    ThermoStat(heatSensor, coolingMechanism, this);
                thermoStat.RunThermoStat();

            }
        }
        public interface IDevice
        {
            double WarningLevelTemperature { get; }
            double EmergencyLevelTemperature { get; }
            void RunDevice();
            void HandleEmergency();
        }

        public class CoolingMechanism : ICoolingMechanism
        {
            public void Off()
            {
                Console.WriteLine();
                Console.WriteLine("Switching cooling mechanism off..");
            }

            public void On()
            {
                Console.WriteLine();
                Console.WriteLine("Switching cooling mechanism on..");
            }
        }

        public interface ICoolingMechanism
        {
            void On();
            void Off();
        }
        public class HeatSensor : IHeatSensor
        {

            double _warningLevel = 0;
            double _emergencyLevel = 0;
            bool _hasReachedWarningTemperature = false;

            public EventHandlerList ListEventDelegates = new EventHandlerList();

            static readonly object _temperatureReachesWarningLevelKey = new object();
            static readonly object _temperatureFallsBelowWarningLevelKey = new object();
            static readonly object _temperatureReachesEmergencyLevelKey = new object();


            private double[] _temperatureData = null;

            public HeatSensor(double warningLevel, double emergencyLevel)
            {
                _warningLevel = warningLevel;
                _emergencyLevel = emergencyLevel;
                SeedData();
            }


            private void MonitorTemperature()
            {
                foreach (var temperature in _temperatureData)
                {
                    Console.ResetColor();
                    Console.WriteLine($"Date Time: {DateTime.Now} , Temperature {temperature}");

                    TemperatureEventArgs e = new TemperatureEventArgs()
                    {
                        Temperature = temperature,
                        CurrentDateTime = DateTime.Now
                    };

                    if(temperature >= _emergencyLevel )
                    {
                        OnTemperatureReachedEmergencyLevel(e);
                    }
                    else if (temperature >= _warningLevel)
                    {
                        _hasReachedWarningTemperature = true;
                        OnTemperatureReachedWarningLevel(e);
                    }
                    else if (temperature < _warningLevel && _hasReachedWarningTemperature)
                    {
                        _hasReachedWarningTemperature = false;
                        OnTemperatureFallsBelowWarningLevel(e);
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }
                 
            private void SeedData()
            {
                _temperatureData = new double[] {16,17,18,26,28,29.5,30,35,22,39,40,56,66,68,72,78 }; 
            }
            protected void OnTemperatureReachedEmergencyLevel(TemperatureEventArgs e)
            {
                EventHandler<TemperatureEventArgs> handler = (EventHandler<TemperatureEventArgs>)ListEventDelegates[_temperatureReachesEmergencyLevelKey];
                handler?.Invoke(this, e);
            }

            protected void OnTemperatureReachedWarningLevel(TemperatureEventArgs e)
            {
                EventHandler<TemperatureEventArgs> handler = (EventHandler<TemperatureEventArgs>)ListEventDelegates[_temperatureReachesWarningLevelKey];
                handler?.Invoke(this, e);
            }

            protected void OnTemperatureFallsBelowWarningLevel(TemperatureEventArgs e)
            {
                EventHandler<TemperatureEventArgs> handler = (EventHandler<TemperatureEventArgs>)ListEventDelegates[_temperatureFallsBelowWarningLevelKey];
                handler?.Invoke(this, e);
            }
             
            event EventHandler<TemperatureEventArgs> IHeatSensor.TemperatureReachedEmergencyLevelEventHandler
            {
                add
                {
                    ListEventDelegates.AddHandler(_temperatureReachesEmergencyLevelKey, value);
                }

                remove
                {
                    ListEventDelegates.RemoveHandler(_temperatureReachesEmergencyLevelKey, value);
                }
            }

            event EventHandler<TemperatureEventArgs> IHeatSensor.TemperatureReachedWarningLevelEventHandler
            {
                add
                {
                    ListEventDelegates.AddHandler(_temperatureReachesWarningLevelKey, value);
                }

                remove
                {
                    ListEventDelegates.RemoveHandler(_temperatureReachesWarningLevelKey, value);
                }
            }

            event EventHandler<TemperatureEventArgs> IHeatSensor.TemperatureFallsBelowWarningLevelEventHandler
            {
                add
                {
                    ListEventDelegates.AddHandler(_temperatureFallsBelowWarningLevelKey, value);
                   
                }

                remove
                {
                    ListEventDelegates.RemoveHandler(_temperatureFallsBelowWarningLevelKey, value);
                }
            }

            public void RunHeatSensor()
            {
                Console.WriteLine("Heat Sensor is running..");
                MonitorTemperature();
                
            }
        }
        public interface IHeatSensor
        {
            
            event EventHandler<TemperatureEventArgs> TemperatureReachedEmergencyLevelEventHandler;
            event EventHandler<TemperatureEventArgs> TemperatureReachedWarningLevelEventHandler;
            event EventHandler<TemperatureEventArgs> TemperatureFallsBelowWarningLevelEventHandler;
            void RunHeatSensor();
        }
        public class TemperatureEventArgs : EventArgs
        {   
            public double Temperature { get; set; }
            public DateTime CurrentDateTime { get; set; }

        }

    }
}