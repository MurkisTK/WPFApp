using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Net.Http;

namespace ServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TcpListener listener = new TcpListener(new IPAddress(new byte[] { 127, 0, 0, 1 }), 8888);
        NetworkStream? stream = null;
        public MainWindow()
        {
            InitializeComponent();
            Application();
            Closing += CancelConnection;
        }

        void Application()
        {
            Task.Run(DataReceive);
        }

        public void CancelConnection(object sender, CancelEventArgs e)
        {
            try
            {
                listener.Stop();
            }
            catch (Exception)
            {


            }
        }

        async Task DataReceive()
        {
            int numberOfMessage = 1;
            StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "OK"; }), DispatcherPriority.Normal, null);
            try
            {
                listener.Start();
                StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Waiting for connection"; }), DispatcherPriority.Normal, null);
                TcpClient tcpclient = await listener.AcceptTcpClientAsync();
                StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Connection succesful"; ; }), DispatcherPriority.Normal, null);
                stream = tcpclient.GetStream();
                while (stream != null)
                {
                    if (tcpclient != null)
                    {
                        var buffer = new List<byte>();
                        int byteread = 0;
                        while ((byteread = stream.ReadByte()) != '\n')
                        {
                            buffer.Add((byte)byteread);
                        }
                        var message = Encoding.UTF8.GetString(buffer.ToArray());
                        if (message == "END")
                        {
                            break;
                        }
                        MetarText.Dispatcher.Invoke(new Action(() => MetarText.Text = message), DispatcherPriority.Normal, null);
                        StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = $"Message {numberOfMessage++} received"; }), DispatcherPriority.Normal, null);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = ex.Message; }), DispatcherPriority.Normal, null);
                Thread.Sleep(1000);
            }
            finally
            {
                StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Connection closed"; }), DispatcherPriority.Normal, null);
                stream = null;
                listener.Stop();
            }
        }
    }
}