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
    public partial class FormSettings : Form
    {
        Form1 MainWin;
        private bool bNew = false;
        
        Dictionary<string, string[]> dict_config = new Dictionary<string, string[]>();

        public FormSettings(Form1 owner)
        {
            MainWin = owner;
            InitializeComponent();

            listViewModbusSettings.Items.Clear();

            listViewModbusSettings.Columns.Add("Имя", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns.Add("ip-адрес", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns.Add("Порт", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns.Add("Адрес счетчика", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns.Add("Потребитель", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns.Add("Договор", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns.Add("Адрес", -2, HorizontalAlignment.Left);
            listViewModbusSettings.Columns[0].Width = 100;
            listViewModbusSettings.Columns[1].Width = 100;
            listViewModbusSettings.Columns[2].Width = 100;
            listViewModbusSettings.Columns[3].Width = 100;
            listViewModbusSettings.Columns[4].Width = 100;
            listViewModbusSettings.Columns[5].Width = 100;
            listViewModbusSettings.Columns[6].Width = 100;
            
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            ReloadForm();
        }

        public void FormSettings_Closed(object sender, EventArgs e)
        {
            MainWin.ReloadComboBox();
            MainWin.formSettings = null;
        }

        private void buttonNew_Click(object sender, EventArgs e)
        {
            bNew = true;
            textBoxName.Enabled = true;
            textBoxIp.Enabled = true;
            textBoxPort.Enabled = true;
            textBoxMeter_addr.Enabled = true;
            textBoxConsumer.Enabled = true;
            textBoxContract.Enabled = true;
            textBoxAddress.Enabled = true;

            textBoxName.Text = "";
            textBoxIp.Text = "";
            textBoxPort.Text = "";
            textBoxMeter_addr.Text = "1";
            textBoxConsumer.Text = "";
            textBoxContract.Text = "";
            textBoxAddress.Text = "";

        }


        public static void CreateXml(ref Dictionary<string, string[]> dict_config)
        {
            XmlDocument xml = new XmlDocument();
            XmlDeclaration xmlDeclaration = xml.CreateXmlDeclaration("1.0", "UTF-8", "no");

            XmlElement root = xml.CreateElement("Options");
            xml.AppendChild(root);

            foreach (var pair in dict_config)
            {
                XmlElement child = xml.CreateElement("meter");
                root.AppendChild(child);

                XmlElement name = xml.CreateElement("name");
                name.InnerText = pair.Key;
                child.AppendChild(name);

                XmlElement ip = xml.CreateElement("ip");
                ip.InnerText = pair.Value[0];
                child.AppendChild(ip);

                XmlElement port = xml.CreateElement("port");
                port.InnerText = pair.Value[1];
                child.AppendChild(port);

                XmlElement meter_addr = xml.CreateElement("meter_addr");
                meter_addr.InnerText = pair.Value[2];
                child.AppendChild(meter_addr);

                XmlElement consumer = xml.CreateElement("consumer");
                consumer.InnerText = pair.Value[3];
                child.AppendChild(consumer);

                XmlElement contract = xml.CreateElement("contract");
                contract.InnerText = pair.Value[4];
                child.AppendChild(contract);

                XmlElement address = xml.CreateElement("address");
                address.InnerText = pair.Value[5];
                child.AppendChild(address);

            }

            xml.InsertBefore(xmlDeclaration, xml.FirstChild);
            //string s = xml.OuterXml;
            //xml.LoadXml(s);
            string fileXML = Application.StartupPath + "\\config.xml";
            xml.Save(fileXML);
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
       
        

        private void listViewModbusSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxName.Enabled = true;
            textBoxIp.Enabled = true;
            textBoxPort.Enabled = true;
            textBoxMeter_addr.Enabled = true;
            textBoxConsumer.Enabled = true;
            textBoxContract.Enabled = true;
            textBoxAddress.Enabled = true;

            if (listViewModbusSettings.SelectedItems.Count > 0)
            {
                bNew = false;
                ListViewItem itiem = listViewModbusSettings.SelectedItems[listViewModbusSettings.SelectedItems.Count - 1];
                if (itiem != null)
                    foreach (ListViewItem lv in listViewModbusSettings.SelectedItems)
                    {
                        textBoxName.Text = lv.SubItems[0].Text;
                        textBoxIp.Text = lv.SubItems[1].Text;
                        textBoxPort.Text = lv.SubItems[2].Text;
                        textBoxMeter_addr.Text = lv.SubItems[3].Text;
                        textBoxConsumer.Text = lv.SubItems[4].Text;
                        textBoxContract.Text = lv.SubItems[5].Text;
                        textBoxAddress.Text = lv.SubItems[6].Text;
                    }
            }

        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (bNew == true)
            {
                if (textBoxName.Text != "")
                {
                    if (!dict_config.ContainsKey(textBoxName.Text))
                    {
                        dict_config[textBoxName.Text] = new string[] { textBoxIp.Text, textBoxPort.Text, textBoxMeter_addr.Text, textBoxConsumer.Text, textBoxContract.Text, textBoxAddress.Text };
                        CreateXml(ref dict_config);
                        ReloadForm();
                    }
                    else
                    {
                        //name already exists
                        throw new InvalidOperationException("This name already exists in configs");
                    }
                }
            }
            else
            {
                dict_config[textBoxName.Text] = new string[] { textBoxIp.Text, textBoxPort.Text, textBoxMeter_addr.Text, textBoxConsumer.Text, textBoxContract.Text, textBoxAddress.Text };
                CreateXml(ref dict_config);
                ReloadForm();
            }
        }

        private void checkInput()
        {
            try
            {
                if (textBoxName.Text == "")
                {
                    //Exception ex = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if ((bNew == false)&(textBoxName.Text != ""))
            {
                dict_config.Remove(textBoxName.Text);
                CreateXml(ref dict_config);
                ReloadForm();
                
            }
        }

        private void ReloadForm()
        {
            bNew = false;

            textBoxName.Enabled = false;
            textBoxIp.Enabled = false;
            textBoxPort.Enabled = false;
            textBoxMeter_addr.Enabled = false;
            textBoxConsumer.Enabled = false;
            textBoxContract.Enabled = false;
            textBoxAddress.Enabled = false;
            textBoxMeter_addr.Text = "1";

            listViewModbusSettings.Items.Clear();
            ParseXML(ref dict_config);
            foreach (var pair in dict_config)
            {
                //Console.WriteLine ("NAME {0} IP {1} PORT {2} ", pair.Key, pair.Value[0], pair.Value[1] );
                ListViewItem item = new ListViewItem(pair.Key.ToString());
                item.SubItems.Add(pair.Value[0].ToString().Trim());
                item.SubItems.Add(pair.Value[1].ToString().Trim());
                item.SubItems.Add(pair.Value[2].ToString().Trim());
                item.SubItems.Add(pair.Value[3].ToString().Trim());
                item.SubItems.Add(pair.Value[4].ToString().Trim());
                item.SubItems.Add(pair.Value[5].ToString().Trim());
                /*item.SubItems.Add(pair.Value[6].ToString().Trim());
                item.SubItems.Add(pair.Value[7].ToString().Trim());
                item.SubItems.Add(pair.Value[8].ToString().Trim());*/
                listViewModbusSettings.Items.Add(item);
            }
        }



    }
}