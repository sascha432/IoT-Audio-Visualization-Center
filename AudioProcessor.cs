﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using ManagedBass;
using ManagedBass.Wasapi;

namespace Analyzer
{
    public class AudioProcessor
    {
        // Event Pattern
        public delegate void AudioAvailableEventHandler(object sender, AudioAvailableEventArgs e);

        public event AudioAvailableEventHandler AudioAvailable;



        private bool _enable;               //enabled status
        private DispatcherTimer _t;         //timer that refreshes the display
        private float[] _fft;               //buffer for fft data
        private double _l, _r;         //progressbars for left and right channel intensity
        private WasapiProcedure _process;        //callback function to obtain data
        private int _lastlevel;             //last output level
        private int _hanctr;                //last output level counter
        private List<byte> _spectrumdata;   //spectrum data buffer

        private bool _initialized;          //initialized flag
        private int devindex;               //used device index

        



        //ctor

        public AudioProcessor(int deviceIndex, bool trimEnd = true)
        {

            _fft = new float[1024];
            _lastlevel = 0;
            _hanctr = 0;
            _t = new DispatcherTimer();
            _t.Tick += _t_Tick;
            _t.Interval = TimeSpan.FromMilliseconds(20); //40hz refresh rate
            _t.IsEnabled = false;
            _l = 0;
            _r = 0;
            _r = ushort.MaxValue;
            _l = ushort.MaxValue;
            _process = new WasapiProcedure(Process);
            _spectrumdata = new List<byte>();

            devindex = deviceIndex;
            _initialized = false;
        }

        public void SwitchDevice(int deviceIndex)
        {
            devindex = deviceIndex;
            _hanctr = 4;
        }

        //flag for enabling and disabling program functionality
        public bool Enable
        {
            get { return _enable; }
            set
            {
                _enable = value;
                if (value)
                {
                    if (!_initialized)
                    {
                        bool result = BassWasapi.Init(devindex, 0, 0, WasapiInitFlags.Buffer, 1f, 0.05f, _process, IntPtr.Zero);
                        if (!result)
                        {
                            var error = Bass.LastError;
                            //MessageBox.Show(error.ToString());
                        }
                        else
                        {
                            _initialized = true;
                        }
                    }
                    BassWasapi.Start();
                }
                else BassWasapi.Stop(true);
                System.Threading.Thread.Sleep(50);
                _t.IsEnabled = value;
            }
        }



        //timer 
        private void _t_Tick(object sender, EventArgs e)
        {
            // get fft data. Return value is -1 on error
            int ret = BassWasapi.GetData(_fft, (int)ManagedBass.DataFlags.FFT2048);
            if (ret < 0) return;

            OnAudioAvailable(_fft);
            _spectrumdata.Clear();


            int level = BassWasapi.GetLevel();
            
            _l = ManagedBass.BitHelper.LoWord(level);
            _r = ManagedBass.BitHelper.HiWord(level);
            if (level == _lastlevel && level != 0) _hanctr++;
            _lastlevel = level;

            //Required, because some programs hang the output. If the output hangs for a 75ms
            //this piece of code re initializes the output
            //so it doesn't make a gliched sound for long.
            if (_hanctr > 3)
            {
                _hanctr = 0;
                _l = 0;
                _r = 0;
                Free();
                Bass.Init(0, 44100, DeviceInitFlags.Default, IntPtr.Zero);
                _initialized = false;
                Enable = true;
            }


        }



        // WASAPI callback, required for continuous recording
        private int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        //cleanup
        public void Free()
        {
            BassWasapi.Free();
            Bass.Free();
        }

        public static List<byte> getSpectrumData(float[] fftData, int bands, double r, double factor)
        {
            //int retBands = bands;
            //bands = Convert.ToInt32(((double)bands) / r);
            int precision = 16;
            var list = new List<byte>();
            int sumSteps = 0;
            int sum = 0;
            var items = getSpectrumData(fftData, 71, factor);
            int totalBands = items.Count;
            double step = (totalBands * precision) / (double)(bands + 2);
            foreach(byte value in items)
            {
                for(int i = 0; i < precision; i++)
                {
                    sum += value;
                    if (++sumSteps > step)
                    {
                        list.Add((byte)(sum / step));
                        sumSteps = 0;
                        sum = 0;
                    }
                }
            }
            //while(list.Count < bands)
            //{
            //    list.Add(0);
            //}
            return list.GetRange(0, bands);
        }

        public static List<byte> getSpectrumData(float[] fftData, int bands, double factor)
        {
            float max = fftData.Max();
            float min = fftData.Min();
            List<byte> result = new List<byte>();


            int x, y;
            int b0 = 0;
            for (x = 0; x < bands; x++)
            {
                float peak = 0;
                int b1 = (int)Math.Pow(2, x * 5 / (bands - 1));
                if (b1 > 1023) b1 = 1023;
                if (b1 <= b0) b1 = b0 + 1;
                for (; b0 < b1; b0++)
                {
                    if (peak < fftData[1 + b0]) peak = fftData[1 + b0];
                }
                y = (int)(Math.Sqrt(peak) * 4 * 255 - 4);
                if (y > 255) y = 255;
                if (y < 0) y = 0;
                result.Add((byte)y);
            }

            return applyFactor(result, factor);
        }

        public static List<byte> applyFactor(List<byte> x, double f)
        {
            List<byte> ret = new List<byte>();
            foreach (byte b in x)
            {
                int t = (int)(((double)b) * f);
                if (t < 0) t = 0;
                else if (t > 255) t = 255;
                ret.Add((byte)t);
            }
            return ret;
        }


        protected void OnAudioAvailable(float[] _toConv)
        {
            AudioAvailableEventHandler audioAvailable = AudioAvailable;
            if (audioAvailable != null) audioAvailable(this, new AudioAvailableEventArgs(_toConv));
            else
            {
                //throw new NullReferenceException("No Handler!");
            }
        }
    }

    public class AudioAvailableEventArgs : EventArgs
    {
        private float[] data;
        public AudioAvailableEventArgs(float[] fftData)
        {
            this.data = fftData;
        }
        public float[] AudioAvailable { get { return data; } }
    }
}