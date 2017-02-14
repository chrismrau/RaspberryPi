
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RgbSensor.Annotations;

namespace RgbSensor
{
    public class RgbSensorViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        RgbData _color = new RgbData();

        public RgbData Color
        {
            get { return _color; }
            set
            {
                if (_color.Equals(value)) return;

                _color = value;
                OnPropertyChanged();
            }
        }

        Tcs34725 _sensor;
        Tcs34725 Sensor
        {
            get { return _sensor; }
            set { _sensor = value; }
        }

        public Boolean? IsLedOn
        {
            get { return Sensor.LedState == Tcs34725.ELedState.On; }
            set
            {
                Sensor.LedState = value.HasValue && value.Value ? Tcs34725.ELedState.On : Tcs34725.ELedState.Off;
                OnPropertyChanged();
            }
        }

        public RgbSensorViewModel(Tcs34725 sensor)
        {
            Sensor = sensor;
            IsLedOn = false;
        }

    }
}
