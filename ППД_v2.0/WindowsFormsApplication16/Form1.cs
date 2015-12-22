using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.PeerToPeer;
using System.ServiceModel;
using System.Configuration;
using System.IO;

namespace P2P
{
    public partial class Form1 : Form
    {
        int b = 0;
        private P2PService localService;
        private string serviceUrl;
        private ServiceHost host;
        private PeerName peerName;
        private PeerNameRegistration peerNameRegistration;
        private byte[] bData = null;
        private byte[] cData = null;
        int encription = 1;
        int compression = 0;
        int processing = 0;
        int dataLength = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            bool isSelected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);

            if (e.Index > -1)
            {
                /* If the item is selected set the background color to SystemColors.Highlight 
                 or else set the color to either WhiteSmoke or White depending if the item index is even or odd */
                Color color = isSelected ? SystemColors.Highlight :
                    e.Index % 2 == 0 ? Color.WhiteSmoke : Color.White;

                // Background item brush
                SolidBrush backgroundBrush = new SolidBrush(color);
                // Text color brush
                SolidBrush textBrush = new SolidBrush(e.ForeColor);

                // Draw the background
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
                // Draw the text
                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, textBrush, e.Bounds, StringFormat.GenericDefault);

                // Clean up
                backgroundBrush.Dispose();
                textBrush.Dispose();
            }
            e.DrawFocusRectangle();
            listBox1.DisplayMember = "DisplayString";
            listBox1.ValueMember = "DisplayString";
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            // Получение конфигурационной информации из app.config
            string port = ConfigurationManager.AppSettings["port"];
            string username = ConfigurationManager.AppSettings["username"];
            string machineName = Environment.MachineName;
            string serviceUrl = null;

            // Установка заголовка окна
            this.Text = string.Format("P2P приложение - {0}", username);

            //  Получение URL-адреса службы с использованием адресаIPv4 
            //  и порта из конфигурационного файла
            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    serviceUrl = string.Format("net.tcp://{0}:{1}/P2PService", address, port);
                    break;
                }
            }

            // Выполнение проверки, не является ли адрес null
            if (serviceUrl == null)
            {
                // Отображение ошибки и завершение работы приложения
                MessageBox.Show(this, "Не удается определить адрес конечной точки WCF.", "Networking Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
            }

            // Регистрация и запуск службы WCF
            localService = new P2PService(this, username);
            host = new ServiceHost(localService, new Uri(serviceUrl));
            NetTcpBinding binding = new NetTcpBinding();
            binding.Security.Mode = SecurityMode.None;
            host.AddServiceEndpoint(typeof(IP2PService), binding, serviceUrl);
            try
            {
                host.Open();
            }
            catch (AddressAlreadyInUseException)
            {
                // Отображение ошибки и завершение работы приложения
                MessageBox.Show(this, "Не удается начать прослушивание, порт занят.", "WCF Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Application.Exit();
            }

            // Создание имени равноправного участника (пира)
            peerName = new PeerName("P2P Sample", PeerNameType.Unsecured);

            // Подготовка процесса регистрации имени равноправного участника в локальном облаке
            peerNameRegistration = new PeerNameRegistration(peerName, int.Parse(port));
            peerNameRegistration.Cloud = Cloud.AllLinkLocal;

            // Запуск процесса регистрации
            peerNameRegistration.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Остановка регистрации
            peerNameRegistration.Stop();

            // Остановка WCF-сервиса
            host.Close();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            // Создание распознавателя и добавление обработчиков событий
            PeerNameResolver resolver = new PeerNameResolver();
            resolver.ResolveProgressChanged +=
                new EventHandler<ResolveProgressChangedEventArgs>(resolver_ResolveProgressChanged);
            resolver.ResolveCompleted +=
                new EventHandler<ResolveCompletedEventArgs>(resolver_ResolveCompleted);

            // Подготовка к добавлению новых пиров
            listBox1.Items.Clear();
            button2.Enabled = false;

            // Преобразование незащищенных имен пиров асинхронным образом
            resolver.ResolveAsync(new PeerName("0.P2P Sample"), 1);
        }

        void resolver_ResolveCompleted(object sender, ResolveCompletedEventArgs e)
        {
            // Сообщение об ошибке, если в облаке не найдены пиры
            if (listBox1.Items.Count == 0)
            {
                listBox1.Items.Add(
                   new PeerEntry
                   {
                       DisplayString = "Пиры не найдены.",
                       ButtonsEnabled = false
                   });
            }
            // Повторно включаем кнопку "обновить"
            button2.Enabled = true;
        }

        void resolver_ResolveProgressChanged(object sender, ResolveProgressChangedEventArgs e)
        {
            PeerNameRecord peer = e.PeerNameRecord;

            foreach (IPEndPoint ep in peer.EndPointCollection)
            {
                if (ep.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    try
                    {
                        string endpointUrl = string.Format("net.tcp://{0}:{1}/P2PService", ep.Address, ep.Port);
                        NetTcpBinding binding = new NetTcpBinding();
                        binding.Security.Mode = SecurityMode.None;
                        IP2PService serviceProxy = ChannelFactory<IP2PService>.CreateChannel(
                            binding, new EndpointAddress(endpointUrl));
                        listBox1.Items.Add(
                           new PeerEntry
                           {
                               PeerName = peer.PeerName,
                               ServiceProxy = serviceProxy,
                               DisplayString = serviceProxy.GetName(),
                               ButtonsEnabled = true
                           });
                    }
                    catch (EndpointNotFoundException)
                    {
                        listBox1.Items.Add(
                           new PeerEntry
                           {
                               PeerName = peer.PeerName,
                               DisplayString = "Неизвестный пир",
                               ButtonsEnabled = false
                           });
                    }
                }
            }
        }

        //private void PeerList_Click(object sender, EventArgs e)
        //{
        //    // Убедимся, что пользователь щелкнул по кнопке с именем MessageButton
        //    if (((Button)e.OriginalSource).Name == "button1")
        //    {
        //        // Получение пира и прокси, для отправки сообщения
        //        PeerEntry peerEntry = ((Button)e.OriginalSource).DataContext as PeerEntry;
        //        if (peerEntry != null && peerEntry.ServiceProxy != null)
        //        {
        //            try
        //            {
        //                peerEntry.ServiceProxy.SendMessage("Привет друг!", ConfigurationManager.AppSettings["username"]);
        //            }
        //            catch (CommunicationException)
        //            {

        //            }
        //        }
        //    }
        //}

        internal void DisplayMessage(string message, string from)
        {
            // Показать полученное сообщение (вызывается из службы WCF)
            //MessageBox.Show(this, message, string.Format("Сообщение от {0}", from),
            //    MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            textBox2.AppendText(((PeerEntry)listBox1.SelectedItem).DisplayString + ": " + message + Environment.NewLine);
        }
        internal void DisplayMessageByte(byte[] message, string from)
        {
            // Показать полученное сообщение (вызывается из службы WCF)
            //MessageBox.Show(this, message, string.Format("Сообщение от {0}", from),
            //    MessageBoxButtons.OK, MessageBoxIcon.Information);
            byte[] fileLength = new byte[8];
            byte[] pocketLength = new byte[4];
            Buffer.BlockCopy(message, 9, pocketLength, 0, 4);

            if (processing == 0)
            {
                Buffer.BlockCopy(message, 1, fileLength, 0, 8);
                cData = new byte[BitConverter.ToInt64(fileLength, 0)];
            }
            int a = BitConverter.ToInt32(pocketLength, 0);
            Buffer.BlockCopy(message, 15, cData, dataLength, BitConverter.ToInt32(pocketLength, 0));
            dataLength += BitConverter.ToInt32(pocketLength, 0);
            processing = 1;
          
            if (message[63999] == 84)
            {
                Stream myStream;
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 2;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if ((myStream = saveFileDialog1.OpenFile()) != null)
                    {
                        myStream.Close();
                    }
                }
                File.WriteAllBytes(saveFileDialog1.FileName, cData);
                dataLength = 0;
                processing = 0;
            }
            b++;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Создание распознавателя и добавление обработчиков событий
            PeerNameResolver resolver = new PeerNameResolver();
            resolver.ResolveProgressChanged +=
                new EventHandler<ResolveProgressChangedEventArgs>(resolver_ResolveProgressChanged);
            resolver.ResolveCompleted +=
                new EventHandler<ResolveCompletedEventArgs>(resolver_ResolveCompleted);

            // Подготовка к добавлению новых пиров
            listBox1.Items.Clear();
            button2.Enabled = false;

            // Преобразование незащищенных имен пиров асинхронным образом
            resolver.ResolveAsync(new PeerName("0.P2P Sample"), 1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PeerEntry peerEntry = listBox1.SelectedItem as PeerEntry;
            if (peerEntry != null && peerEntry.ServiceProxy != null)
            {
                try
                {
                    if (bData != null)
                    {
                        byte[] fileLength = new byte[8];
                        byte[] pocketLength = new byte[4];

                        ///////////////////////////////////////////////////////////////////////////////////////////
                        //|стартовый байт|длина файла|длина данных пакета|шифрование|сжатие|данные|конечный байт|//
                        ///////////////////////////////////////////////////////////////////////////////////////////

                        if (bData.Length <= 63984) //Размер файла меньше максимаьного размера блока данных пакета
                        {
                            byte[] message = new byte[64000];
                            fileLength = BitConverter.GetBytes((long)bData.Length);
                            pocketLength = BitConverter.GetBytes(bData.Length);
                            message[0] = 170;                                   //Стартовый байт
                            Buffer.BlockCopy(fileLength, 0, message, 1, 8);     //Длина файла
                            Buffer.BlockCopy(pocketLength, 0, message, 9, 4);   //Длина данных пакета
                            message[13] = 1;                                    //Шифрование
                            message[14] = 0;                                    //Сжатие
                            message[63999] = 84;                                //Конечный байт

                            Buffer.BlockCopy(bData, 0, message, 15, bData.Length);
                            peerEntry.ServiceProxy.SendMessageByte(message, ConfigurationManager.AppSettings["username"]);
                            bData = null;
                            label1.Text = "";
                        }
                        else //Размер файла больше максимального размера блока данных пакета
                        {
                            byte[] nolik = { 0, 0, 0, 0 };
                            fileLength = BitConverter.GetBytes((long)bData.Length);
                            pocketLength = BitConverter.GetBytes(63984);
                            //long sou = BitConverter.ToInt64(fileLength, 0);

                            byte[] message = new byte[64000];
                            message[0] = 170;                                   
                            Buffer.BlockCopy(fileLength, 0, message, 1, 8);     
                            Buffer.BlockCopy(pocketLength, 0, message, 9, 4);   
                            message[13] = 1;                                    
                            message[14] = 0;                                    
                            message[63999] = 85;                                

                            int count = 0;
                            int length = bData.Length;
                            for (int i = 0; i < bData.Length / 63984 + 1; i++)
                            {

                                if (length - 63984 >= 0)
                                {
                                    pocketLength = BitConverter.GetBytes(63984);
                                    Buffer.BlockCopy(pocketLength, 0, message, 9, 4); 
                                    Buffer.BlockCopy(bData, count, message, 15, 63984);
                                    if (length == 63984)
                                    {
                                        message[63999] = 84;
                                    }
                                    peerEntry.ServiceProxy.SendMessageByte(message, ConfigurationManager.AppSettings["username"]);
                                    Buffer.BlockCopy(nolik, 0, message, 9, 4);
                                    count += 63984;
                                    length -= 63984;
                                }
                                else //Последний пакет
                                {
                                    message = new byte[64000];
                                    message[0] = 170;
                                    Buffer.BlockCopy(fileLength, 0, message, 1, 8);
                                    pocketLength = BitConverter.GetBytes(length);
                                    Buffer.BlockCopy(pocketLength, 0, message, 9, 4);
                                    message[13] = 1;
                                    message[14] = 0;
                                    message[63999] = 84;
                                    Buffer.BlockCopy(bData, count, message, 15, length);
                                    peerEntry.ServiceProxy.SendMessageByte(message, ConfigurationManager.AppSettings["username"]);
                                }
                            }

                            bData = null;
                            label1.Text = "";
                        }
                    }
                    else
                    {
                        peerEntry.ServiceProxy.SendMessage(textBox1.Text, ConfigurationManager.AppSettings["username"]);
                        textBox2.AppendText(ConfigurationManager.AppSettings["username"] + ": " + textBox1.Text + Environment.NewLine);
                        textBox1.Text = "";
                    }                                       
                }
                catch (CommunicationException)
                {

                }
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                           bData = File.ReadAllBytes(openFileDialog1.FileName);
                           label1.Text = openFileDialog1.SafeFileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Остановка регистрации
            peerNameRegistration.Stop();

            // Остановка WCF-сервиса
            host.Close();
        }

        private void параметрыПередачиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

      

        
    }
}
