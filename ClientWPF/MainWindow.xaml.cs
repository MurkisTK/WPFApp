using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClientWPF
{
    public partial class MainWindow : Window
    {
        bool cloudsValueLocker = false;
        string metarNote = "";
        List<RunwayVisibility> runwayList = new();
        List<Clouds> clouds = new();
        TcpClient tcpClient = new TcpClient();
        NetworkStream? networkStream = null;

        public MainWindow()
        {
            InitializeComponent();

            Task.Run(StartConnection);
            Task.Run(ServerResponse);

            // Lists
            RunwayVisibilities.ItemsSource = runwayList;
            CloudsBox.ItemsSource = clouds;
            runwayList.Add(new RunwayVisibility());
            clouds.Add(new Clouds());
            RunwayVisibilities.Items.Refresh();
            RunwayVisibilities.SelectedIndex = 0;
            CloudsBox.Items.Refresh();
            CloudsBox.SelectedIndex = 0;

            // Disable components
            NoneClouds.IsEnabled = false;
            NcdClouds.IsEnabled = false;
            UndefinedType.IsEnabled = false;

            // Initial METAR Note
            MetarField();

            // Events
            Closing += CancelConnection;
        }

        async Task<bool> StartConnection()
        {
            if (tcpClient.Connected)
            {
                return true;
            }
            try
            {
                await tcpClient.ConnectAsync(new IPAddress(new byte[] { 127, 0, 0, 1 }), 8888);
                networkStream = tcpClient.GetStream();
                StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Успешное подключение"; }), DispatcherPriority.Normal, null);
                return true;
            }
            catch(Exception)
            {
                StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Подключение не удалось"; }), DispatcherPriority.Normal, null);
                tcpClient = new TcpClient();
                return false;
            }
        }

        async Task ServerResponse()
        {
            while (true)
            {
                    try
                    {
                        byte[] data = new byte[512];
                        if(networkStream != null)
                    {
                        await networkStream.ReadAsync(data);
                        string str = Encoding.UTF8.GetString(data);
                        if (str == "END\n")
                        {
                            StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Подключение завершено"; }), DispatcherPriority.Normal, null);
                        }
                    }                                          
                    }
                    catch (Exception)
                    {

                    }                   
            }
        }

        public void CancelConnection(object sender, CancelEventArgs e)
        {
            try
            {
                string message = "END\n";
                if (tcpClient.Connected)
                {
                    var formatted = Encoding.UTF8.GetBytes(message);
                    networkStream?.Write(formatted);
                }
                tcpClient.Close();
            }
            catch (Exception) {

            }
        }
        string GetMetarNote()
        {
            string result = "";

            // Result note
            result += IdentifierGroupNote();
            result += WindNote();
            result += RunwayVisibilityNote();
            result += CloudsNote();
            result += TemperatureNote();
            result += PressureNote();

            return result;
        }

        private void MetarField()
        {
            MetarEndNote.Text = GetMetarNote();
        }

        private string IdentifierGroupNote()
        {
            string identifierGroup = "";

            // identifierGroup
            identifierGroup += "METAR ";
            identifierGroup += (CorCheckBox.IsChecked == true ? "COR " : "");
            identifierGroup += NameField.Text.ToUpper() + " ";
            identifierGroup += string.Format("{0:d2}", DayField.Text);
            identifierGroup += string.Format("{0:d2}", HourField.Text);
            identifierGroup += string.Format("{0:d2}", MinuteField.Text);
            identifierGroup += "Z ";
            identifierGroup += (NilCheckBox.IsChecked == true ? "NIL " : "");
            identifierGroup += (AutoCheckBox.IsChecked == true ? "AUTO " : "");

            return identifierGroup;
        }

        private string WindNote()
        {
            string wind = "";
            double averageWindSpeed, maxWindSpeed, minWindDirection, averageWindDirection, maxWindDirection;

            // wind
            averageWindSpeed = double.Parse(AverageWindSpeed.Text);
            maxWindSpeed = double.Parse(MaxWindSpeed.Text);
            minWindDirection = double.Parse(MinWindDirection.Text);
            averageWindDirection = double.Parse(AverageWindDirection.Text);
            maxWindDirection = double.Parse(MaxWindDirection.Text);

            if ((averageWindSpeed < 0.5 && MetersButton.IsChecked == true) || (averageWindSpeed < 1 && KnotsButton.IsChecked == true))
            {
                wind += "00000" + (KnotsButton.IsChecked == true ? "KT" : "MPS");
            }
            else
            {
                if (maxWindDirection - minWindDirection > 180 ||
                    (((averageWindSpeed < 1.5 && MetersButton.IsChecked == true) || (averageWindSpeed < 3 && KnotsButton.IsChecked == true)) && maxWindDirection - minWindDirection >= 60 && maxWindDirection - minWindDirection <= 180)
                   )
                {
                    wind += "VRB";
                }
                else
                {
                    wind += string.Format("{0:d3}", (int)(Math.Round(averageWindDirection / 10, 0, MidpointRounding.AwayFromZero) * 10));
                }
                if ((Math.Floor(averageWindSpeed) >= 50 && MetersButton.IsChecked == true) || (Math.Floor(averageWindSpeed) >= 100 && KnotsButton.IsChecked == true))
                {
                    wind += 'P';
                    if (MetersButton.IsChecked == true)
                    {
                        wind += 49;
                    }
                    else
                    {
                        wind += 99;
                    }
                }
                else
                {
                    wind += string.Format("{0:d2}", (int)Math.Round(averageWindSpeed, 0, MidpointRounding.AwayFromZero));
                    if ((maxWindSpeed - averageWindSpeed >= 5 && MetersButton.IsChecked == true) || (maxWindSpeed - averageWindSpeed >= 10 && KnotsButton.IsChecked == true))
                    {
                        wind += "G" + string.Format("{0:d2}", (int)Math.Round(maxWindSpeed, 0, MidpointRounding.AwayFromZero));
                    }
                }
                wind += (KnotsButton.IsChecked == true ? "KT" : "MPS");
                if (maxWindDirection - minWindDirection >= 60 && maxWindDirection - minWindDirection <= 180 &&
                    !((averageWindSpeed < 1.5 && MetersButton.IsChecked == true) || (averageWindSpeed < 3 && KnotsButton.IsChecked == true))
                   )
                {
                    wind += " " + string.Format("{0:d3}", (int)(Math.Round(minWindDirection / 10, 0, MidpointRounding.AwayFromZero) * 10)) + "V" + string.Format("{0:d3}", (int)(Math.Round(maxWindDirection / 10, 0, MidpointRounding.AwayFromZero) * 10));
                }
            }
            wind += " ";

            return wind;
        }

        private string RunwayVisibilityNote()
        {
            string runwayVisibilityNote = "";
            List<RunwayVisibility> cloneRunwayList = new List<RunwayVisibility>();
            foreach (RunwayVisibility rv in runwayList)
            {
                cloneRunwayList.Add((RunwayVisibility)rv.Clone());
            }
            cloneRunwayList.Sort();

            // Visibility
            foreach (RunwayVisibility rv in cloneRunwayList)
            {
                runwayVisibilityNote += rv.ToString() + " ";
            }

            return runwayVisibilityNote;
        }

        private string CloudsNote()
        {
            string cloudsNote = "";
            List<Clouds> cloneClouds = new List<Clouds>();
            foreach (Clouds c in clouds)
            {
                cloneClouds.Add((Clouds)c.Clone());
            }
            cloneClouds.Sort();

            // Clouds
            foreach (Clouds c in cloneClouds)
            {
                cloudsNote += c.ToString() + " ";
            }

            return cloudsNote;
        }

        private string TemperatureNote()
        {
            string temperatureNote = "";

            double tAir, tDewPoint;
            tAir = double.Parse(TemperatureAir.Text);
            tDewPoint = double.Parse(TemperatureDewPoint.Text);
            temperatureNote += tAir < 0 ? "M" : "";
            temperatureNote += string.Format("{0:d2}", (int)Math.Abs(Math.Round(tAir + 0.000001, 0, MidpointRounding.AwayFromZero)));
            temperatureNote += "/";
            temperatureNote += tDewPoint < 0 ? "M": "";
            temperatureNote += string.Format("{0:d2}", (int)Math.Abs(Math.Round(tDewPoint + 0.000001, 0, MidpointRounding.AwayFromZero)));
            temperatureNote += " ";

            return temperatureNote;
        }
        private string PressureNote()
        {
            string pressureNote = "";

            double pAtmosphere;
            pAtmosphere = double.Parse(Pressure.Text);
            pressureNote += "Q";
            pressureNote += string.Format("{0:d4}", (int)Math.Floor(pAtmosphere));

            return pressureNote;
        }

        private bool CheckErrors()
        {
            bool result = true;
            if (!CheckIdentifierGroup() || !CheckWind() || !CheckRunwayVisibilities() || !CheckClouds() || !CheckTemperature() || !CheckPressure())
            {
                result = false;
            }
            return result;
        }

        private bool CheckIdentifierGroup()
        {
            bool result = true;
            int i1;
            // IdentifierGroup
            // NameField
            if (Regex.IsMatch(NameField.Text, "^[A-Z]+$") && NameField.Text.Length == 4)
            {
                NameField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            else
            {
                NameField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                result = false;
            }
            // DayField
            if (int.TryParse(DayField.Text, out i1) && i1 > 0 && i1 < 32)
            {
                DayField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            else
            {
                DayField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                result = false;
            }
            // HourField
            if (int.TryParse(HourField.Text, out i1) && i1 > -1 && i1 < 24)
            {
                HourField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            else
            {
                HourField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                result = false;
            }
            // MinuteField
            if (int.TryParse(MinuteField.Text, out i1) && i1 > -1 && i1 < 60)
            {
                MinuteField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            else
            {
                MinuteField.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                result = false;
            }
            return result;
        }

        private bool CheckWind()
        {
            bool result = true;
            double d1, d2;
            // Wind
            // Average wind speed
            if (double.TryParse(AverageWindSpeed.Text, out d1) &&
                ((KnotsButton.IsChecked == true && d1 > 0 && d1 < 120) || (MetersButton.IsChecked == true && d1 > 0 && d1 < 60)))
            {
                AverageWindSpeed.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                // Max wind speed
                if (double.TryParse(MaxWindSpeed.Text, out d2) &&
                    ((KnotsButton.IsChecked == true && d2 > 0 && d2 < 120) || (MetersButton.IsChecked == true && d2 > 0 && d2 < 60)) &&
                    d2 >= d1)
                {
                    MaxWindSpeed.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                }
                else
                {
                    MaxWindSpeed.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    result = false;
                }
            }
            else
            {
                AverageWindSpeed.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                result = false;
            }
            // Average wind direction
            if (double.TryParse(AverageWindDirection.Text, out d1) &&
                d1 >= 0 && d1 <= 360)
            {
                AverageWindDirection.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                // Max wind direction
                if (double.TryParse(MaxWindDirection.Text, out d2) &&
                    d2 >= 0 && d2 <= 360 && d2 >= d1)
                {
                    MaxWindDirection.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                }
                else
                {
                    MaxWindDirection.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    result = false;
                }
                // Min wind direction
                if (double.TryParse(MinWindDirection.Text, out d2) &&
                    d2 >= 0 && d2 <= 360 && d2 <= d1)
                {
                    MinWindDirection.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                }
                else
                {
                    MinWindDirection.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    result = false;
                }
            }
            else
            {
                AverageWindDirection.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                result = false;
            }
            return result;
        }

        private bool CheckRunwayVisibilities()
        {
            bool result = true;
            for (int i = 0; i < runwayList.Count - 1; i++)
            {
                if (result == false)
                {
                    break;
                }
                for (int j = i + 1; j < runwayList.Count; j++)
                {
                    if (int.Parse(runwayList[i].RunwayNumber) == int.Parse(runwayList[j].RunwayNumber))
                    {
                        BorderRunwayVisibilities.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                        result = false;
                        break;
                    }
                }
            }
            if (result == true)
            {
                BorderRunwayVisibilities.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            return result;
        }

        private bool CheckClouds()
        {
            bool result = true;
            AutoCheckBox.BorderBrush = new SolidColorBrush(Colors.Black);
            BorderCloudsBox.BorderBrush = new SolidColorBrush(Colors.Black);

            List<Clouds> cloneClouds = new List<Clouds>();
            foreach (Clouds c in clouds)
            {
                cloneClouds.Add((Clouds)c.Clone());
            }
            cloneClouds.Sort();
            if (cloneClouds.Count == 1 && AutoCheckBox.IsChecked == true)
            {
                return true;
            }
            bool repeated = false;
            int num = 0;
            if(AutoCheckBox.IsChecked == false)
            {
                foreach(Clouds c in cloneClouds)
                {
                    if (c.Type == CloudTypes.Undefined || c.Count == CloudCount.None || c.Count == CloudCount.NCD)
                    {
                        AutoCheckBox.BorderBrush = new SolidColorBrush(Colors.Red);
                        return false;
                    }
                }
            }
            foreach(Clouds c in cloneClouds)
            {
                if(c.Type == CloudTypes.Undefined)
                {
                    num++;
                }
            }
            if(!(num == 0 || num == cloneClouds.Count))
            {
                BorderCloudsBox.BorderBrush = new SolidColorBrush(Colors.Red);
                return false;
            }
            if(cloneClouds.Count > 1)
            {
                for(int i = 1; i < cloneClouds.Count; i++)
                {
                    if (cloneClouds[i].Distance == "///" && cloneClouds[i-1].Distance != "///")
                    {
                        BorderCloudsBox.BorderBrush = new SolidColorBrush(Colors.Red);
                        return false;
                    }
                    if (cloneClouds[i].Count == cloneClouds[i - 1].Count)
                    {
                        if (repeated || cloneClouds.Count != 4)
                        {
                            BorderCloudsBox.BorderBrush = new SolidColorBrush(Colors.Red);
                            return false;
                        }
                        if (cloneClouds[i].Type == CloudTypes.None || cloneClouds[i].Type == cloneClouds[i - 1].Type)
                        {
                            BorderCloudsBox.BorderBrush = new SolidColorBrush(Colors.Red);
                            return false;
                        }
                        repeated = true;
                    }
                    if (cloneClouds[i-1].Distance != "///" && cloneClouds[i].Distance != "///" && int.Parse(cloneClouds[i].Distance) <= int.Parse(cloneClouds[i-1].Distance))
                    {
                        BorderCloudsBox.BorderBrush = new SolidColorBrush(Colors.Red);
                        return false;
                    }
                }
            }
            return result;
        }

        private bool CheckTemperature()
        {
            bool result = true;
            double t;
            if (double.TryParse(TemperatureAir.Text, out t) && t > -30 && t < 45)
            {
                TemperatureAir.BorderBrush = new SolidColorBrush(Colors.Black);
            }
            else
            {
                TemperatureAir.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }
            if (double.TryParse(TemperatureAir.Text, out t) && t > -20 && t < 40)
            {
                TemperatureDewPoint.BorderBrush = new SolidColorBrush(Colors.Black);
            }
            else
            {
                TemperatureDewPoint.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }
            return result;
        }

        private bool CheckPressure()
        {
            bool result = true;
            double t;
            if (double.TryParse(Pressure.Text, out t) && t > 500 && t < 3000)
            {
                Pressure.BorderBrush = new SolidColorBrush(Colors.Black);
            }
            else
            {
                Pressure.BorderBrush = new SolidColorBrush(Colors.Red);
                result = false;
            }
            return result;
        }

        private void MakeNote_Click(object sender, RoutedEventArgs e)
        {
            if (CheckErrors())
            {
                MetarField();
            }
        }

        private void SendNote_Click(object sender, RoutedEventArgs e)
        {
            metarNote = MetarEndNote.Text;
            Task.Run(SendData);
        }

        async Task SendData()
        {
            bool t = await StartConnection();
            if (t)
            {
                try
                {
                    string message = "";
                    message += metarNote;
                    message += "\n";
                    var formatted = Encoding.UTF8.GetBytes(message);
                    if (networkStream != null)
                    {
                        await networkStream.WriteAsync(formatted);
                        StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Данные успешно отправлены"; }), DispatcherPriority.Normal, null);
                    }
                }
                catch (Exception)
                {
                    StatusBarText.Dispatcher.Invoke(new Action(() => { StatusBarText.Text = "Ошибка при отправке"; }), DispatcherPriority.Normal, null);
                }
            }
        }

        private void RunwayAdd_Click(object sender, RoutedEventArgs e)
        {
            if (runwayList.Count == 4)
            {
                return;
            }
            runwayList.Add(new RunwayVisibility());
            RunwayVisibilities.Items.Refresh();
        }

        private void RunwayDelete_Click(object sender, RoutedEventArgs e)
        {
            if (runwayList.Count < 2)
            {
                return;
            }
            runwayList.RemoveAt(RunwayVisibilities.SelectedIndex);
            RunwayVisibilities.Items.Refresh();
            RunwayVisibilities.SelectedIndex = 0;
        }

        private void SaveRunway_Click(object sender, RoutedEventArgs e)
        {
            int num;
            int index = RunwayVisibilities.SelectedIndex;
            if (int.TryParse(RunwayNumber.Text, out num) && num > 0)
            {
                RunwayNumber.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            else
            {
                RunwayNumber.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                return;
            }
            if (int.TryParse(RunwayVisibility.Text, out num) && num > 0)
            {
                RunwayVisibility.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            }
            else
            {
                RunwayVisibility.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                return;
            }
            runwayList[index].RunwayNumber = RunwayNumber.Text;
            runwayList[index].ParralelIdentifier = LeftParallel.IsChecked == true ? "L" : RightParallel.IsChecked == true ? "R" : CentralParallel.IsChecked == true ? "C" : "";
            runwayList[index].Visibility = RunwayVisibility.Text;
            runwayList[index].Tendency = UpTendency.IsChecked == true ? "U" : DownTendency.IsChecked == true ? "D" : NormalTendency.IsChecked == true ? "N" : "";
            RunwayVisibilities.SelectedIndex = -1;
            RunwayVisibilities.SelectedIndex = index;
            RunwayVisibilities.Items.Refresh();
        }

        private void AutoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AutoCheckBox.IsChecked == true)
            {
                cloudsValueLocker = true;
                UndefinedType.IsEnabled = true;
                if (clouds.Count == 1)
                {
                    NoneClouds.IsEnabled = true;
                    NcdClouds.IsEnabled = true;
                }
            }
            else
            {
                cloudsValueLocker = false;
                NoneClouds.IsEnabled = false;
                NcdClouds.IsEnabled = false;
                UndefinedType.IsEnabled = false;
                if (NcdClouds.IsChecked == true || NoneClouds.IsChecked == true) FewClouds.IsChecked = true;
                if (UndefinedType.IsChecked == true) NoneType.IsChecked = true;
            }
        }

        private void CloudsAdd_Click(object sender, RoutedEventArgs e)
        {
            if (clouds.Count == 4)
            {
                return;
            }
            if (clouds[0].Count == CloudCount.VV || clouds[0].Count == CloudCount.None || clouds[0].Count == CloudCount.NCD || clouds[0].Count == CloudCount.NSC)
            {
                return;
            }
            clouds.Add(new Clouds());
            CloudsBox.Items.Refresh();
            VvClouds.IsEnabled = false;
            NscClouds.IsEnabled = false;
            NoneClouds.IsEnabled = false;
            NcdClouds.IsEnabled = false;
            if (NcdClouds.IsChecked == true || NoneClouds.IsChecked == true || VvClouds.IsChecked == true || NscClouds.IsChecked == true) FewClouds.IsChecked = true;
        }

        private void CloudsDelete_Click(object sender, RoutedEventArgs e)
        {
            if (clouds.Count < 2)
            {
                return;
            }
            clouds.RemoveAt(CloudsBox.SelectedIndex);
            CloudsBox.Items.Refresh();
            CloudsBox.SelectedIndex = 0;
            if (clouds.Count == 1)
            {
                VvClouds.IsEnabled = true;
                NscClouds.IsEnabled = true;
                if (cloudsValueLocker == true)
                {
                    NoneClouds.IsEnabled = true;
                    NcdClouds.IsEnabled = true;
                }
            }
        }

        private void SaveCloud_Click(object sender, RoutedEventArgs e)
        {
            string distance = CloudsDistance.Text;
            CloudCount cloudCount;
            CloudTypes cloudTypes;
            int index = CloudsBox.SelectedIndex;
            cloudCount = FewClouds.IsChecked == true ? CloudCount.FEW : SctClouds.IsChecked == true ? CloudCount.SCT : BknClouds.IsChecked == true ? CloudCount.BKN :
                OvcClouds.IsChecked == true ? CloudCount.OVC : VvClouds.IsChecked == true ? CloudCount.VV : NscClouds.IsChecked == true ? CloudCount.NSC :
                NcdClouds.IsChecked == true ? CloudCount.NCD : CloudCount.None;
            cloudTypes = CBType.IsChecked == true ? CloudTypes.CB : TCUType.IsChecked == true ? CloudTypes.TCU : NoneType.IsChecked == true ? CloudTypes.None : CloudTypes.Undefined;
            if ((int.TryParse(distance, out int num) && ((cloudCount == CloudCount.VV && num > 0 && num < 21) || (cloudCount != CloudCount.VV && num > 0 && num < 51))) || distance == "///")
            {
                CloudsDistance.BorderBrush = new SolidColorBrush(Colors.Black);
            }
            else
            {
                CloudsDistance.BorderBrush = new SolidColorBrush(Colors.Red);
                return;
            }
            clouds[index].Count = cloudCount;
            if(cloudCount == CloudCount.VV || cloudCount == CloudCount.NSC || cloudCount == CloudCount.NCD)
            {
                clouds[index].Type = CloudTypes.None;
            }
            else
            {
                clouds[index].Type = cloudTypes;
            }            
            clouds[index].Distance = distance == "///" ? "///" : num.ToString();
            CloudsBox.SelectedIndex = -1;
            CloudsBox.SelectedIndex = index;
            CloudsBox.Items.Refresh();
        }
    }

    public class RunwayVisibility() : IComparable<RunwayVisibility>, ICloneable
    {
        public string RunwayNumber { get; set; } = "1";
        public string ParralelIdentifier { get; set; } = "";
        public string Visibility { get; set; } = "100";
        public string Tendency { get; set; } = "";
        public int CompareTo(RunwayVisibility? other)
        {
            return int.Parse(this.RunwayNumber).CompareTo(int.Parse(other?.Visibility));
        }
        public object Clone()
        {
            return new RunwayVisibility() { RunwayNumber = RunwayNumber, ParralelIdentifier = ParralelIdentifier, Tendency = Tendency, Visibility = Visibility };
        }
        public override string ToString()
        {
            string result = "";
            int num = int.Parse(Visibility);
            if (num <= 400)
            {
                while (num - 25 >= 0)
                {
                    num -= 25;
                }
                num = int.Parse(Visibility) - num;
            }
            else if (num <= 800)
            {
                while (num - 50 >= 0)
                {
                    num -= 50;
                }
                num = int.Parse(Visibility) - num;
            }
            else
            {
                while (num - 100 >= 0)
                {
                    num -= 100;
                }
                num = int.Parse(Visibility) - num;
            }
            result += "R";
            result += string.Format("{0:d2}", int.Parse(RunwayNumber));
            result += ParralelIdentifier;
            result += "/";
            result += num <= 50 ? "M0050" : num >= 2000 ? "P2000" : string.Format("{0:d4}", num);
            result += Tendency;
            return result;
        }
    }

    public class Clouds() : IComparable<Clouds>, ICloneable
    {
        public CloudCount Count { get; set; } = CloudCount.FEW;
        public string Distance { get; set; } = "5";
        public CloudTypes Type { get; set; } = CloudTypes.None;
        public int CompareTo(Clouds? other)
        {
            int num;
            num = this.Count.CompareTo(other?.Count);
            if (num == 0)
            {
                if (other?.Distance == "///" || this.Distance == "///")
                {
                    return this.Distance.CompareTo(other?.Distance);
                }
                else
                {
                    return int.Parse(this.Distance).CompareTo(int.Parse(other?.Distance));
                }
            }
            return num;
        }
        public object Clone()
        {
            return new Clouds() { Count = this.Count, Distance = this.Distance, Type = this.Type };
        }
        public override string ToString()
        {
            string result = "";
            switch (Count)
            {
                case CloudCount.FEW: { result += "FEW"; break; }
                case CloudCount.SCT: { result += "SCT"; break; }
                case CloudCount.BKN: { result += "BKN"; break; }
                case CloudCount.OVC: { result += "OVC"; break; }
                case CloudCount.VV: { result += "VV"; break; }
                case CloudCount.NSC: { result += "NSC"; return result; }
                case CloudCount.NCD: { result += "NCD"; return result; }
                case CloudCount.None: { result += "///"; break; }
            }
            if (int.TryParse(Distance, out int num))
            {
                result += string.Format("{0:d3}", num);
            }
            else if (Distance == "///")
            {
                result += "///";
            }
            else
            {
                result += "";
            }
            if (Count == CloudCount.VV)
            {
                return result;
            }
            switch (Type)
            {
                case CloudTypes.TCU: { result += "TCU"; break; }
                case CloudTypes.CB: { result += "CB"; break; }
                case CloudTypes.Undefined: { result += "///"; break; }
                case CloudTypes.None: { result += ""; break; }
            }
            return result;
        }
    }

    public enum CloudCount
    {
        None = 0, FEW = 1, SCT = 2, BKN = 3, OVC = 4, VV = 5, NSC = 6, NCD = 7
    }

    public enum CloudTypes
    {
        Undefined = 0, CB = 1, TCU = 2, None = 3
    }
}