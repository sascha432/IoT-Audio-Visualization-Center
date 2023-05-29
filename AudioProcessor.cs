using System;
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
        private static int _fftSize;
        private float[] _fft;               //buffer for fft data
        private double _l, _r;         //progressbars for left and right channel intensity
        private WasapiProcedure _process;        //callback function to obtain data
        private int _lastlevel;             //last output level
        private int _hanctr;                //last output level counter
        private List<byte> _spectrumdata;   //spectrum data buffer

        private bool _initialized;          //initialized flag
        private int devindex;               //used device index
        private readonly int _lastLevel;
        private static int _sLastLevel = 0;

        //ctor

        public AudioProcessor(int deviceIndex, bool trimEnd = true)
        {
            _fftSize = 2048;
            _fft = new float[_fftSize];
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
                else
                {
                    BassWasapi.Stop(true);
                }
                System.Threading.Thread.Sleep(50);
                _t.IsEnabled = value;
            }
        }

        //timer 
        private void _t_Tick(object sender, EventArgs e)
        {
            // get fft data. Return value is -1 on error
            int len = 0;
            switch (_fftSize)
            {
                case 8192:
                    _fftSize = 8192;
                    len = (int)ManagedBass.DataFlags.FFT8192;
                    break;
                case 4096:
                    _fftSize = 4096;
                    len = (int)ManagedBass.DataFlags.FFT4096;
                    break;
                case 2048:
                    _fftSize = 2048;
                    len = (int)ManagedBass.DataFlags.FFT2048;
                    break;

            }
            int ret = BassWasapi.GetData(_fft, len);
            if (ret < 0)
            {
                return;
            }

            OnAudioAvailable(_fft);
            _spectrumdata.Clear();

            int level = BassWasapi.GetLevel();
            _sLastLevel = level;

            _l = ManagedBass.BitHelper.LoWord(level);
            _r = ManagedBass.BitHelper.HiWord(level);
            if (level == _lastlevel && level != 0)
            {
                _hanctr++;
            }
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
            _sLastLevel = 0;
            BassWasapi.Free();
            Bass.Free();
        }

        public static List<byte> getSpectrumData(float[] fftData, int bands, double r, double factor)
        {
            if (r == 1.0)
            {
                return getSpectrumData(fftData, bands, factor);
            }
            else
            {
                int retBands = bands;
                bands = Convert.ToInt32(((double)bands) / r);
                return getSpectrumData(fftData, bands, factor).GetRange(0, retBands);
            }
        }

        private static int getFftBandIndex(float frequency)
        {
            double f = 44100 / 2.0;
            return (int)((frequency / f) * (_fftSize / 2));
        }

        public static byte getLeftLevel()
        {
            return (byte)(ManagedBass.BitHelper.LoWord(_sLastLevel) >> 7);
        }

        public static byte getRightLevel()
        {
            return (byte)(ManagedBass.BitHelper.HiWord(_sLastLevel) >> 7);
        }

        public static bool getLevelError()
        {
            return _sLastLevel == -1;
        }

        public static List<byte> getSpectrumData(float[] fftData, int bands, double factor)
        {
            //float max = fftData.Max();
            //float min = fftData.Min();
            List<byte> result = new List<byte>();

            bands = 32;

            //// max frequency for logarithmic scale
            double fIncr = 16800 / bands;
            double maxFrequency = fIncr;
            double px = 1.1;
            double mul = maxFrequency / Math.Pow(px, bands);

            int x, y;
            int b0 = 0;

            List<float> b = new List<float>();

            for (x = 0; x < bands; x++)
            {
                float frequency = (float)(Math.Pow(px, x) * mul); // logarithmic spectrum up to maxFrequency
                float peak = 0;
                b.Add(frequency);

                maxFrequency += fIncr;
                mul = maxFrequency / Math.Pow(px, bands);

                int b1 = getFftBandIndex(frequency);
                if (b0 == b1)
                {
                    peak = fftData[b0];
                }
                else
                {
                    for (; b0 < b1; b0++)
                    {
                        if (peak < fftData[b0])
                        {
                            peak = fftData[b0];
                        }
                    }
                }
                y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
                if (y < 0)
                {
                    y = 0;
                }
                else if (y > 255)
                {
                    y = 255;
                }
                result.Add((byte)y);
            }

            // wo do not want to display those bands
            while (result.Count < 71)
            {
                result.Add(0);
            }

            return factor != 1.0 ? applyFactor(result, factor) : result;
        }

        public static List<byte> applyFactor(List<byte> x, double f)
        {
            List<byte> ret = new List<byte>();
            foreach (byte b in x)
            {
                int t = (int)(((double)b) * f);
                if (t < 0)
                {
                    t = 0;
                }
                else if (t > 255)
                {
                    t = 255;
                }
                ret.Add((byte)t);
            }
            return ret;
        }


        protected void OnAudioAvailable(float[] _toConv)
        {
            AudioAvailableEventHandler audioAvailable = AudioAvailable;
            if (audioAvailable != null)
            {
                audioAvailable(this, new AudioAvailableEventArgs(_toConv));
            }
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