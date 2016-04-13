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
using Demo.PLC_Handler;
using Demo.BL_BackEnd;
using System.Timers;
using System.ComponentModel;
using Forms = System.Windows.Forms;
using System.Windows.Threading;
using System.Globalization;
using System.Threading;
using System.Windows.Media.Animation;


namespace Demo.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        System.Timers.Timer timer;
        private int _counter;
        private Boolean connectionStatus;
        private int numOfRetransmissions;

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
        private bool isOff;
        protected virtual void OnPropertyChanged(string name)
        {
            var handler = System.Threading.Interlocked.CompareExchange(ref PropertyChanged, null, null);
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion


        public MainWindow()
        {
             InitializeComponent();
             connectionStatus = false;
            String[] sPortsList = SerialPort.GetPortNames();
            sPort = null;
            plcHandler = null;
            counter = 0;

            timer = new System.Timers.Timer(60000);
            timer.Elapsed += timer_Elapsed;
            timer.Start();

            Binding b = new Binding("counter");
            b.Mode = BindingMode.OneWay;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            numOfRetransmissions = 0;
            //tmp_counter.SetBinding(Label.ContentProperty, b);

            DataContext = this;
            foreach (String sPortName in sPortsList)
            {
                comboPorts.Items.Add(sPortName);
            }
            isOff = true;
        }


        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            btnRefresh_MouseDown(null, null);
        }

        private void btnRed_MouseDown(object sender, MouseButtonEventArgs e)
        {
           btnRed.Visibility = Visibility.Collapsed;
           btnGreen.Visibility = Visibility.Visible;
            if (plcHandler != null)
            {
                if (plcHandler.waitingForResponse()) return;
                IPLCCommand cmd = new StatusCommand((byte)DevProtocol.ConnectionOpCode.running);
                plcHandler.send(cmd);
                isOff = true;
                btnRed.Visibility = Visibility.Collapsed;
                btnGreen.Visibility = Visibility.Visible;
            }
        }

        private void btnGreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            btnRed.Visibility = Visibility.Visible;
            btnGreen.Visibility = Visibility.Collapsed;
            if (plcHandler != null)
            {
                if (plcHandler.waitingForResponse()) return;
                IPLCCommand cmd = new StatusCommand((byte)DevProtocol.ConnectionOpCode.pause);
                plcHandler.send(cmd);
                isOff = false;
                btnGreen.Visibility = Visibility.Collapsed;
                btnRed.Visibility = Visibility.Visible;
            }
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
            }
        }

        private void disconnectPort()
        {
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
            Storyboard sb = this.FindResource("Spin360") as Storyboard;
            PLCCommand cmd = null;
            if (plcHandler.hasNext()) cmd = (PLCCommand)plcHandler.next();
            if (cmd == null) return;
            //on recieve message make the connection success.
            new Thread(delegate()
            {
                connectionStatus = true;
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    (Action)(() =>
                     sb.RepeatBehavior = new RepeatBehavior(1)));
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    (Action)(() =>
                     sb.Begin()));

                Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                (Action)(() =>
                statusLabel.Content = "Connection Success"));

                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    (Action)(() =>
                    statusLabel.Visibility = Visibility.Visible));
                Thread.Sleep(3000);
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal,
                    (Action)(() =>
                    statusLabel.Visibility = Visibility.Collapsed));


            }).Start();
            numOfRetransmissions = 0;
            
            while(counter <= cmd.getMeter()){
             Thread.Sleep(1);
             populateCounterImages(counter);
             this.counter++;
            }

        }

        private void populateCounterImages(int counter)
        {
            int unity = counter % 10;
            int dozen = (counter / 10) % 10;
            int hundreds = (counter / 100) % 10;
            int thousands = (counter / 1000) % 10;

        Application.Current.Dispatcher.BeginInvoke(
        DispatcherPriority.Normal,
        (Action)(() =>
        firstt.Source = new BitmapImage(new Uri("Pics/" + thousands + ".png", UriKind.Relative))));

        Application.Current.Dispatcher.BeginInvoke(
        DispatcherPriority.Normal,
        (Action)(() =>
            secondd.Source = new BitmapImage(new Uri("Pics/" + hundreds + ".png", UriKind.Relative))));

        Application.Current.Dispatcher.BeginInvoke(
        DispatcherPriority.Normal,
        (Action)(() =>
            thirdd.Source = new BitmapImage(new Uri("Pics/" + dozen + ".png", UriKind.Relative))));

        Application.Current.Dispatcher.BeginInvoke(
        DispatcherPriority.Normal,
        (Action)(() =>
            forthh.Source = new BitmapImage(new Uri("Pics/" + unity + ".png", UriKind.Relative))));

        }


        private void Switch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isOff)
            {
                btnGreen_MouseDown(null, null);
            }
            else
            {
                btnRed_MouseDown(null, null);
            }
        }

        private void btnRefresh_MouseDown(object sender, MouseButtonEventArgs e){
            Storyboard sb = this.FindResource("Spin360") as Storyboard;
            try
            {
                sb.RepeatBehavior = new RepeatBehavior(1);
                sb.Begin();
            }
            catch (Exception ex) { }
            if (plcHandler != null && plcHandler.waitingForResponse()) return;
            timer.Stop();
            timer.Start();
            if (plcHandler == null) return;
            IPLCCommand cmd = new RefreshCommand();
            try
            {
                sb.RepeatBehavior = RepeatBehavior.Forever;
                sb.Begin();
            }
            catch (Exception ex) { }
            plcHandler.waitForResponse();
            plcHandler.send(cmd);
            new Thread(delegate()
            {
                try
                {
                    connectionStatus = false;
                    Thread.Sleep(3000);
                    if (!connectionStatus)
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() =>
                            statusLabel.Content = "Connection Failed"));

                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() =>
                            statusLabel.Visibility = Visibility.Visible));

                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() =>
                             sb.RepeatBehavior = new RepeatBehavior(0)));
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() =>
                             sb.Begin()));

                        if (numOfRetransmissions < 2)
                        {
                            plcHandler.stopWaitForResponse();
                            numOfRetransmissions++;
                            Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            (Action)(() =>
                               btnRefresh_MouseDown(null, null)));
                        }
                        else
                        {
                            plcHandler.stopWaitForResponse();
                            numOfRetransmissions = 0;
                        }
                    }

                    Thread.Sleep(3000);
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        (Action)(() =>
                        statusLabel.Visibility = Visibility.Collapsed));


                }
                catch (Exception ex) { }

            }).Start();
        }

        private void comboPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


}
    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 1.0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is double)
                    result *= (double)values[i];
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new Exception("Not implemented");
        }

    } 

}
