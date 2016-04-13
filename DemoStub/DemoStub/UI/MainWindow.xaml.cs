using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Ports;
using DemoStub.PLC_Handler;
using DemoStub.BL_BackEnd;
using System.Timers;
using System.ComponentModel;

namespace DemoStub.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        Timer timer;
        public int counter
        {
            get { return _counter; }
            set 
            { 
                _counter = value;
                OnPropertyChanged("counter");
            }
        }
        SerialPort sPort;
        IHandler plcHandler;

#region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
#endregion
        private void btnRed_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (plcHandler != null)
            {
                timer.Start();
                btnRed.Visibility = Visibility.Collapsed;
                btnGreen.Visibility = Visibility.Visible;
                status = 1;
            }
        }

        private void btnGreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (plcHandler != null)
            {
                timer.Stop();
                btnGreen.Visibility = Visibility.Collapsed;
                btnRed.Visibility = Visibility.Visible;
                status = 2;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            String[] sPortsList = SerialPort.GetPortNames();
            sPort = null;
            plcHandler = null;
            counter = 0;

            Binding b = new Binding("counter");
            b.Mode = BindingMode.OneWay;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

            tmp_counter.SetBinding(Label.ContentProperty, b);
            DataContext = this;



            timer = new Timer(1000);
            timer.Elapsed += timer_Elapsed;
            

            foreach (String sPortName in sPortsList)
            {
                comboPorts.Items.Add(sPortName);
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            counter++;
            //tmp_counter.Text = ""+counter;
            //tmp_counter.UpdateLayout();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (btnConnection.Content.ToString().Equals("connect"))
            {
                if(connectPort())
                    btnConnection.Content = "disconnect";
            }
            else
            {
                disconnectPort();
                btnConnection.Content = "connect";
                //if (btnGreen.Visibility == Visibility.Visible) timer.Start();
            }
        }

        private void disconnectPort()
        {
            btnGreen_MouseDown(null, null);
            if (sPort != null) sPort.Close();
            sPort = null;
            plcHandler = null;
            comboPorts.AllowDrop = true;
        }

        private bool connectPort()
        {
                if (comboPorts.SelectedItem == null)
                {
                    return false;
                }
                sPort = new SerialPort();
                sPort.BaudRate = 57600;
                sPort.PortName = (String)comboPorts.SelectedItem;
                try
                {
                    sPort.Open();
                    plcHandler = new Handler(sPort);
                    plcHandler.onPacketReceived += recieveMessage;
                    comboPorts.AllowDrop = false;
                    return true;
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp.ToString());
                }
                return false;
          }

        private void recieveMessage()
        {
            IPLCCommand cmd = null;
            if (plcHandler.hasNext()) cmd = plcHandler.next();
            if (cmd == null) return;
            //tmp_counter.InvalidateVisual();
            byte[] command = cmd.toByteArray();
            if (command[0] == (byte)DevProtocol.OpCode.setConnection)
            {
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    if (command[1] == (byte)DevProtocol.ConnectionOpCode.pause)
                    {
                        btnGreen.Visibility = Visibility.Collapsed;
                        btnRed.Visibility = Visibility.Visible;
                        timer.Stop();
                        status = 2;
                    }
                    else
                    {
                        btnRed.Visibility = Visibility.Collapsed;
                        btnGreen.Visibility = Visibility.Visible;
                        timer.Start();
                        status = 1;
                    }
                }));
                status = Convert.ToInt32((byte)DevProtocol.ConnectionOpCode.running);


            }
            else if (command[0] == (byte)DevProtocol.OpCode.getData)
            {
                PLCCommand sendCmd = new PLCCommand(counter, status);
                if (plcHandler != null) plcHandler.send(sendCmd);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }


        public int _counter { get; set; }

        public int status { get; set; }
    }
}
