using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;


namespace elf_client
{
    public partial class Form1 : Form
    {
        //meters connections
        const byte address = 1;
        short[] values = new short[32];
        const ushort start = 256;
        const ushort registers = 120;
        
        //dictionary with config
        Dictionary<string, string[]> dict_config = new Dictionary<string, string[]>();

        private bool client_running;
        private Socket client;
        private IPAddress ip;
        private int port;
        private List<Thread> threads = new List<Thread>();
        string modbusStatus;

        public FormSettings formSettings = null;

        public Form1()
        {
            InitializeComponent();
            listViewModbus.Columns.Add("Дата", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("T,час", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("Q,Гкал", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("М1,т", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("М2,т", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("Gm1,т/ч", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("Gm2,т/ч", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("t1,C", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("t2,C", -2, HorizontalAlignment.Left);
            listViewModbus.Columns.Add("бt", -2, HorizontalAlignment.Left);
            listViewModbus.Columns[0].Width = 100;
            listViewModbus.Columns[1].Width = 100;
            listViewModbus.Columns[2].Width = 100;
            listViewModbus.Columns[3].Width = 100;
            listViewModbus.Columns[4].Width = 100;
            listViewModbus.Columns[5].Width = 100;
            listViewModbus.Columns[6].Width = 100;
            listViewModbus.Columns[7].Width = 100;
            listViewModbus.Columns[8].Width = 100;
            listViewModbus.Columns[9].Width = 100;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DateTime pickedDate = new DateTime(dateTimePickerStart.Value.Year, dateTimePickerStart.Value.Month, 1);
            dateTimePickerStart.Value = pickedDate.AddDays(-1);
            dateTimePickerEnd.Value = dateTimePickerEnd.Value.AddDays(-1);
            ReloadComboBox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var pair in dict_config)
            {
                if (pair.Key == comboBoxMeter.Text)
                {
                    ip = IPAddress.Parse(pair.Value[0]);
                    port = int.Parse(pair.Value[1]);
                    //address = 1;
                    
                }
            }

            client_running = true;

            DateTime val1 = dateTimePickerStart.Value;
            DateTime val2 = dateTimePickerEnd.Value;
            double days_cnt = (val2 - val1).TotalDays;           
            int DayInterval = 1;
            //dictionary for daily values
            Dictionary<DateTime, string[]> dict_date = new Dictionary<DateTime, string[]>();
            dict_date[val1] = new string[] { "", "", "", "", "", "" };

            List<DateTime> dateList = new List<DateTime>();
            dateList.Add(val1);
            while (val1.AddDays(DayInterval) <= val2)
            {
                val1 = val1.AddDays(DayInterval);
                dateList.Add(val1);
                dict_date[val1] = new string[] { "", "", "", "", "", "" };
            }
            //dictionary for monthly values
            Dictionary<DateTime, string[]> dict_mon = new Dictionary<DateTime, string[]>();
            //dictionary for result
            Dictionary<DateTime, string[]> dict_result = new Dictionary<DateTime, string[]>();
            //date for monthly values
            DateTime date_mon = new DateTime();

            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(ip, port);
            
            GetDailyValues(address, start, registers, ref values, ref dict_date, ref dict_mon, ref dict_result, days_cnt,ref date_mon);
            
            //throw new InvalidOperationException(date_mon.ToString());
            DateTime date_mon_copy = date_mon;
            DateTime date_start = date_mon.AddDays(+1);
            DateTime date_end = date_mon.AddDays(-1);
            
            //serach for first day (montly values) try .. except => date not found in results
            int cnt = 0;
            try
            {
                int c = 0;
                foreach (var pair in dict_date)
                {
                    if (pair.Key == date_mon)
                    {
                        dict_result[pair.Key] = new string[] { dict_mon[date_mon][0], dict_mon[date_mon][1], dict_mon[date_mon][2], dict_mon[date_mon][3], (double.Parse(pair.Value[2]) / 24).ToString(), (double.Parse(pair.Value[3]) / 24).ToString(), dict_mon[date_mon][4], dict_mon[date_mon][5], (double.Parse(dict_mon[date_mon][4]) - double.Parse(dict_mon[date_mon][5])).ToString() };
                        c++;
                    }
                }
                if (c == 0)
                {
                    throw new Exception("Montly values not found in given interval");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            cnt++;
            //create result dictionary
            while (cnt <= days_cnt)
            {

                foreach (var pair in dict_date)
                {
                    if (pair.Key == date_end)
                    {
                        double q1 = 0;
                        if (pair.Value[1] != "")
                        {
                            q1 = (double.Parse(dict_result[date_mon][1]) - double.Parse(pair.Value[1]));
                        }
                        double gm1 = 0;
                        double m1 = 0;
                        if (pair.Value[2] != "")
                        {
                            m1 = (double.Parse(dict_result[date_mon][2]) - double.Parse(pair.Value[2]));
                            gm1 = double.Parse(pair.Value[2]) / 24;
                        }
                        double m2 = 0;
                        double gm2 = 0;
                        if (pair.Value[3] != "")
                        {
                            m2 = (double.Parse(dict_result[date_mon][3]) - double.Parse(pair.Value[3]));
                            gm2 = double.Parse(pair.Value[3]) / 24;
                        }
                        double t = 0;
                        if (pair.Value[0] != "")
                        {
                            t = (double.Parse(dict_result[date_mon][0]) - double.Parse(pair.Value[0]));
                        }

                        double t1 = 0;
                        if (pair.Value[4] != "")
                        {
                            t1 = double.Parse(pair.Value[4]);
                        }
                        double t2 = 0;
                        if (pair.Value[5] != "")
                        {
                            t2 = double.Parse(pair.Value[5]);
                        }

                        dict_result[pair.Key] = new string[] { t.ToString(), q1.ToString(), m1.ToString(), m2.ToString(), gm1.ToString(), gm2.ToString(), t1.ToString(), t2.ToString(), (t1 - t2).ToString() };
                        
                        cnt++;
                        date_end = date_end.AddDays(-1);
                        date_mon = date_mon.AddDays(-1);
                        break;
                    }
                }

                foreach (var pair in dict_date)
                {
                    if (pair.Key == date_start)
                    {

                        double q1 = 0;
                        if (pair.Value[1] != "")
                        {
                            q1 = (double.Parse(dict_result[date_mon_copy][1]) + double.Parse(pair.Value[1]));
                        }
                        double m1 = 0;
                        double gm1 = 0;
                        if (pair.Value[2] != "")
                        {
                            m1 = (double.Parse(dict_result[date_mon_copy][2]) + double.Parse(pair.Value[2]));
                            gm1 = double.Parse(pair.Value[2]) / 24;
                        }
                        double m2 = 0;
                        double gm2 = 0;
                        if (pair.Value[3] != "")
                        {
                            m2 = (double.Parse(dict_result[date_mon_copy][3]) + double.Parse(pair.Value[3]));
                            gm2 = double.Parse(pair.Value[3]) / 24;
                        }
                        double t = 0;
                        if (pair.Value[0] != "")
                        {
                            t = (double.Parse(dict_result[date_mon_copy][0]) + double.Parse(pair.Value[0]));
                        }

                        double t1 = 0;
                        if (pair.Value[4] != "")
                        {
                            t1 = double.Parse(pair.Value[4]);
                        }
                        double t2 = 0;
                        if (pair.Value[5] != "")
                        {
                            t2 = double.Parse(pair.Value[5]);
                        }

                        dict_result[pair.Key] = new string[] { t.ToString(), q1.ToString(), m1.ToString(), m2.ToString(), gm1.ToString(), gm2.ToString(), t1.ToString(), t2.ToString(), (t1 - t2).ToString() };

                        cnt++;
                        date_start = date_start.AddDays(+1);
                        date_mon_copy = date_mon_copy.AddDays(+1);
                        break;
                    }
                }

            }

            DateTime first_key = dateTimePickerEnd.Value;
            DateTime last_key = dateTimePickerStart.Value;
            //clear listview
            listViewModbus.Items.Clear();
            //print result
            dateList.ForEach(delegate(DateTime name)
            {
                foreach (var pair in dict_result)
                {
                    if (pair.Key == name)
                    {
                        if (first_key == dateTimePickerEnd.Value)
                        {
                            first_key = pair.Key;
                        }
                        last_key = pair.Key;

                        double t =  double.Parse(pair.Value[0]);
                        double q1 = double.Parse(pair.Value[1]);
                        double m1 = double.Parse(pair.Value[2]);
                        double m2 = double.Parse(pair.Value[3]);
                        double gm1 = double.Parse(pair.Value[4]);
                        double gm2 = double.Parse(pair.Value[5]);
                        double t1 = double.Parse(pair.Value[6]);
                        double t2 = double.Parse(pair.Value[7]);
                        double tb = double.Parse(pair.Value[8]);
                        
                        ListViewItem item = new ListViewItem(pair.Key.ToString("d"));
                        item.SubItems.Add(string.Format("{0:N2}", t));
                        item.SubItems.Add(string.Format("{0:N1}", q1));
                        item.SubItems.Add(string.Format("{0:N2}", m1));
                        item.SubItems.Add(string.Format("{0:N2}", m2));
                        item.SubItems.Add(string.Format("{0:N3}", gm1));
                        item.SubItems.Add(string.Format("{0:N3}", gm2));
                        item.SubItems.Add(string.Format("{0:N1}", t1));
                        item.SubItems.Add(string.Format("{0:N1}", t2));
                        item.SubItems.Add(string.Format("{0:N2}", tb));
                        listViewModbus.Items.Add(item);
                    }
                }

            });

            //first-last
            string [] first_val = dict_result[first_key];
            string[] last_val = dict_result[last_key];
            double res_t = double.Parse(last_val[0]) - double.Parse(first_val[0]);
            double res_q1 = double.Parse(last_val[1]) - double.Parse(first_val[1]);
            double res_m1 = double.Parse(last_val[2]) - double.Parse(first_val[2]);
            double res_m2 = double.Parse(last_val[3]) - double.Parse(first_val[3]);

            ListViewItem res_item = new ListViewItem("Итого");
            res_item.SubItems.Add(string.Format("{0:N2}", res_t));
            res_item.SubItems.Add(string.Format("{0:N1}", res_q1));
            res_item.SubItems.Add(string.Format("{0:N2}", res_m1));
            res_item.SubItems.Add(string.Format("{0:N2}", res_m2));
            res_item.SubItems.Add("");
            res_item.SubItems.Add("");
            res_item.SubItems.Add("");
            res_item.SubItems.Add("");
            res_item.SubItems.Add("");
            listViewModbus.Items.Add(res_item);

        }

        public static byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            //For uppercase A-F letters:
            return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
        
        private void BuildMessage(byte address, byte type, ushort start, ushort registers, ref byte[] message)
        {
            byte[] CRC = new byte[2];
            message[0] = address;
            message[1] = type;
            message[2] = (byte)(start >> 8);
            message[3] = (byte)start;
            message[4] = (byte)(registers >> 8);
            message[5] = (byte)registers;
            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }

        private void BuildSetMessage(byte address, string cmd, DateTime date)
        {
           
            int month = date.Month;
            int day = date.Day;
            int year = int.Parse(date.Year.ToString().Substring(2, 2));
            string m = month.ToString("X2");
            string d = day.ToString("X2");
            string y = year.ToString("X2");
            
            byte[] CRC = new byte[2];
            byte[] message = new byte[8];
            //string hex = "0110000000070E0D0A1F00000000" + cmd + "0001000000030000";
            string hex = "0110000000070E" + y + m + d + "00000000" + cmd + "0001000000" + "62" + "0000";
            //string hex = "0110000000070E" + y + m + d + "00000000" + cmd + "03C0000000" + "62" + "0000";
            message = StringToByteArray(hex);
           
            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
            try
            {
                client.Send(message);
                System.Threading.Thread.Sleep(500);
                byte[] rcv = new byte[8];

                client.Receive(rcv);
                textBox1.Text += ByteArrayToString(message);
                textBox1.Text += Environment.NewLine;
            }
            finally
            {
            }

            
        }

        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            //return hex;
            return hex.Replace("-", "");
        }

        public static float HexStringToFloat(string hex)
        {
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                //raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                raw[raw.Length - i - 1] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            float f = BitConverter.ToSingle(raw, 0);
            return f;
        }

        public void ParseXML(ref Dictionary<string, string[]> dict_config)
        {
            string fileXML = Application.StartupPath + "\\config.xml";
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {

                    xmlDoc.Load(fileXML);

                }
                catch (System.IO.FileNotFoundException)
                {
                    XmlTextWriter xmlWriter = new XmlTextWriter(fileXML, System.Text.Encoding.UTF8);
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8' standalone='no'");
                    xmlWriter.WriteStartElement("Options");
                    xmlWriter.Close();
                    xmlDoc.Load(fileXML);
                }

                XmlNode root = xmlDoc.DocumentElement;
                XmlNodeList nodeList = xmlDoc.SelectNodes("/Options/meter");

                foreach (XmlNode singleItem in nodeList)
                {
                    string name = singleItem.SelectSingleNode("name").InnerText;
                    string ip = singleItem.SelectSingleNode("ip").InnerText;
                    string port = singleItem.SelectSingleNode("port").InnerText;
                    string meter_addr = singleItem.SelectSingleNode("meter_addr").InnerText;
                    string consumer = singleItem.SelectSingleNode("consumer").InnerText;
                    string contract = singleItem.SelectSingleNode("contract").InnerText;
                    string address = singleItem.SelectSingleNode("address").InnerText;
                    dict_config[name] = new string[] { ip, port, meter_addr, consumer, contract, address };
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }    

        public void ParseResult(string packet,Dictionary<DateTime, string []> dict_date)
        {
            float year = Convert.ToInt64(packet.Substring(6, 2), 16);
            float month = Convert.ToInt64(packet.Substring(8, 2), 16);
            float day = Convert.ToInt64(packet.Substring(10, 2), 16);
            float hours = HexStringToFloat(packet.Substring(26, 8));
            float q1 = HexStringToFloat(packet.Substring(34, 8));
            float g1o = HexStringToFloat(packet.Substring(50, 8));
            float g1p = HexStringToFloat(packet.Substring(58, 8));
            float t1o = HexStringToFloat(packet.Substring(66, 8));
            float t1p = HexStringToFloat(packet.Substring(74, 8));

            float g1_24 = g1o / 24;
            float g2_24 = g1p / 24;

            float t = t1o - t1p;

            textBox1.Text += Environment.NewLine;
            textBox1.Text += "YEAR " + year + " MON " + month + " DAY " + day + " HRS " + hours + " Q1 " + q1 + " G1o " + g1o + " G1p " + g1p + " T1o " + t1o + " T1p " + t1p;
            textBox1.Text += Environment.NewLine;

            string date = day + "-" + month + "-" + year;
            
            string year_str = "20" + year.ToString();
            DateTime value = new DateTime(int.Parse(year_str), int.Parse(month.ToString()), int.Parse(day.ToString()));
            dict_date[value] = new string[] { hours.ToString(), q1.ToString(), g1o.ToString(), g1p.ToString(), t1o.ToString(), t1p.ToString() };
        }


        public void ParseMonResult(string packet, Dictionary<DateTime, string[]> dict_mon, ref DateTime date_mon)
        {
            float year = Convert.ToInt64(packet.Substring(6, 2), 16);
            float month = Convert.ToInt64(packet.Substring(8, 2), 16);
            float day = Convert.ToInt64(packet.Substring(10, 2), 16);
            float hours = HexStringToFloat(packet.Substring(26, 8));
            float q1 = HexStringToFloat(packet.Substring(34, 8));
            float g1o = HexStringToFloat(packet.Substring(50, 8));
            float g1p = HexStringToFloat(packet.Substring(58, 8));
            float t1o = HexStringToFloat(packet.Substring(66, 8));
            float t1p = HexStringToFloat(packet.Substring(74, 8));
            
            textBox1.Text += Environment.NewLine;
            textBox1.Text += packet;
            textBox1.Text += Environment.NewLine;
            textBox1.Text += "MONTLY= YEAR " + year + " MON " + month + " DAY " + day + " HRS " + hours + " Q1 " + q1 + " G1o " + g1o + " G1p " + g1p + " T1o " + t1o + " T1p " + t1p;
            textBox1.Text += Environment.NewLine;
            
            string year_str = "20" + year.ToString();
            //throw new Exception("YEAR "+ year_str + "MON "+ month.ToString() + "DAY " + day.ToString()); if incorrect values
            try
            {
                DateTime value = new DateTime(int.Parse(year_str), int.Parse(month.ToString()), int.Parse(day.ToString()));
                dict_mon[value] = new string[] { hours.ToString(), q1.ToString(), g1o.ToString(), g1p.ToString(), t1o.ToString(), t1p.ToString() };
                date_mon = value;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Montly values not found in given interval\n" + ex.ToString());
            }
            
        }


        /*
        public int SetRegister(ushort reg, ushort val)
        {
            int result = 0;
            byte[] message = new byte[8];
            ushort reg_start = reg;
            ushort reg_val = val;
            BuildMessage(address, (byte)6, reg_start, reg_val, ref message);
            try
            {
                //System.Threading.Thread.Sleep(1000);
                client.Send(message);
                System.Threading.Thread.Sleep(500);
                byte[] rcv = new byte[8];

                client.Receive(rcv);
                textBox1.Text += ByteArrayToString(rcv);
                textBox1.Text += Environment.NewLine;
            }
            finally
            {
            }
            return result;
        }*/

        private void buttonExcel_Click(object sender, EventArgs e)
        {
            if (listViewModbus.Items.Count == 0)
            {
                MessageBox.Show("Нет элементов для экспорта в excel",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.ApplicationClass();
            excelApp.Visible = true;
            
            BringToFront();
            excelApp.Application.Workbooks.Add(Type.Missing);
            excelApp.Columns.ColumnWidth = 10;
            //dict_config[name] = new string[] { ip, port, meter_addr, consumer, contract, address };
            Microsoft.Office.Interop.Excel.Range excelCellName = excelApp.get_Range("C1", "C1");
            excelCellName.Value2 = "ВЕДОМОСТЬ УЧЕТА ТЕПЛОВОЙ ЭНЕРГИИ";
            excelCellName = excelApp.get_Range("A2", "A2");
            excelCellName.Value2 = "Потребитель: " + dict_config[comboBoxMeter.Text][3] + "Договор: " + dict_config[comboBoxMeter.Text][4];
            excelCellName = excelApp.get_Range("A3", "A3");
            excelCellName.Value2 = "Адрес: " + dict_config[comboBoxMeter.Text][5];
            excelCellName = excelApp.get_Range("A4", "A4");
            excelCellName.Value2 = "Расчетный месяц: " + dateTimePickerEnd.Value.ToString("MMMM");
            

            int i = 1;
            int i2 = 7;
            int i3 = 1;

            foreach (ColumnHeader col in listViewModbus.Columns)
            {
                excelApp.Cells[i2 - 1, i3] = col.Text; i3++;
            }


            foreach (ListViewItem lvi in listViewModbus.Items)
            {
                i = 1;
                foreach (ListViewItem.ListViewSubItem lvs in lvi.SubItems)
                {
                    excelApp.Cells[i2, i] = lvs.Text;
                    i++;
                }
                i2++;
            }
        }

        public static void Receive(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int received = 0;
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout!");
                try
                {
                    received += socket.Receive(buffer, offset + received, size - received, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Thread.Sleep(30);
                    }
                    else
                        //MessageBox.Show(ex.ToString());
                        throw ex;
                }
            } while (received < size);
        }
       
        public bool GetDailyValues(byte address, ushort start, ushort registers, ref short[] values, ref Dictionary<DateTime, string[]> dict_date, ref Dictionary<DateTime, string[]> dict_mon, ref Dictionary<DateTime, string[]> dict_result,double days,ref DateTime date_mon)
        {
            if (client_running)
            {
                byte[] message = new byte[8];
                byte[] response = new byte[5 + 2 * registers];
                DateTime date = dateTimePickerStart.Value;

                BuildSetMessage(address, "1C",date);

                BuildMessage(address, (byte)4, start, registers, ref message);

                byte[] recv = new byte[245];
                client.Send(message);
                
                Receive(client, recv, 0, 245, 10000);
                textBox1.Text += Environment.NewLine;

                ParseMonResult(ByteArrayToString(recv),dict_mon,ref date_mon);
                
                BuildSetMessage(address, "1B",date);
                System.Threading.Thread.Sleep(500);

                try
                {
                    int ii = 0;
                    
                    while (ii < days)
                    {
                        byte[] rcv = new byte[245];
                        client.Send(message);
                        
                        Receive(client, rcv, 0, 245, 10000);
                        bool result = CheckResponse(rcv);
                        textBox1.Text += result;
                        
                        //client.Receive(rcv);
                        string packet = ByteArrayToString(rcv);
                        ParseResult(packet,dict_date);
                        ii++;
                    }
                    //var lines = dict_date.Select(kv => kv.Key + ": " + kv.Value.ToString());
                    //textBox2.Text += string.Join(Environment.NewLine, lines);
                }
                catch (Exception err)
                {
                    modbusStatus = "Ошибка при чтении: " + err.Message;
                    textBox1.Text = modbusStatus;
                    return false;
                }
                //Evaluate message:
                if (response.Length > 0)
                {
                    /* Return requested register values:
                    for (int i = 0; i < (response.Length - 5) / 2; i++)
                    {
                        values[i] = response[2 * i + 3];
                        values[i] <<= 8;
                        values[i] += response[2 * i + 4];
                    } */
                    modbusStatus = "Чтение выполнено!";
                    //string str = ByteArrayToString(i.ToArray());
                    //textBox1.Text = str;
                    //label1.Text=response;
                    return true;
                }
                else
                {
                    modbusStatus = "Ошибка CRC";
                    textBox1.Text = modbusStatus;
                    return false;
                }
            }
            else
            {
                modbusStatus = "Порт связи закрыт!";
                textBox1.Text = modbusStatus;
                return false;
            }
        }



        //}

        private bool CheckResponse(byte[] response)
        {
            
            byte[] CRC = new byte[2];
            GetCRC(response, ref CRC);
            if (CRC[0] == response[response.Length - 2] && CRC[1] == response[response.Length - 1])
                return true;
            else
                return false;
        }

        private void GetCRC(byte[] message, ref byte[] CRC)
        {

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (formSettings == null)
            {
                formSettings = new FormSettings(this);
                formSettings.Show();
            }
            else
            {
                formSettings.BringToFront();
            }
            //}
            //else
            //{
            //    formSettings.WindowState = FormWindowState.Normal;
            //    formSettings.BringToFront();
            //}
        }

        public void ReloadComboBox()
        {
            comboBoxMeter.Items.Clear();
            dict_config.Clear();

            ParseXML(ref dict_config);

            foreach (var pair in dict_config)
            {
                comboBoxMeter.Items.Add(pair.Key);
            }
            comboBoxMeter.SelectedIndex = 0;
        }

        

        

    }
}