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
using System.Diagnostics;


namespace P2P
{
    public partial class MainForm : Form
    {
        static byte RotLeft(byte val, byte lShift, byte rShift)
        {
            return (byte)((val << lShift) | (val >> rShift));
        }
        //вычисляет контрольную сумму от buffer
        static byte CS(byte[] buffer)
        {
            byte bits = 8;
            byte lShift = 2;
            byte rShift = (byte)(bits - lShift);
            byte res = 0;
            byte index = 0;
            int count = buffer.Length;

            while (count-- > 0)
                res = (byte)(RotLeft(res, lShift, rShift) ^ buffer[index++]);

            return RotLeft(res, lShift, rShift);
        }


        private P2PService localService;        
        private ServiceHost host;
        private PeerName peerName;
        private PeerNameRegistration peerNameRegistration;
        public string username; //имя пользователя
        public string portnumber; //номер порта
        public string filename; //имя передаваемого/получаемого файла
        private byte[] bData = null; //массив передаваемых данных
        private byte[] cData = null; //массив получаемых данных
        public byte encription = 1; // показывает есть ли шифрование при передаче
        public byte compression = 0; // показывает есть ли сжатие при передаче
        public byte[] key; //ключ для шифрования        
        int processing = 0;
        int dataLength = 0;
        int start = 0;

        public delegate void ConError();
        public static ConError ConErrorHandler;



        public MainForm()
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
            label1.Text = "";
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
                    serviceUrl = string.Format("net.tcp://{0}:{1}/P2PService", address, portnumber);
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
            peerNameRegistration = new PeerNameRegistration(peerName, int.Parse(portnumber));
            peerNameRegistration.Cloud = Cloud.AllLinkLocal;

            // Запуск процесса регистрации
            peerNameRegistration.Start();
        }

        private void RefreshButton_Click()
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

  


        //получение переданного сообщения
        internal void DisplayMessage(string message, string from)
        {
            
            if (!message.Equals(0x00 + "peertest" + 0xff, StringComparison.OrdinalIgnoreCase))
            {
                PeerEntry peerEntry = listBox1.SelectedItem as PeerEntry;
                if (peerEntry != null && peerEntry.ServiceProxy != null)
                {
                    try
                    {
                        textBox2.AppendText(((PeerEntry)listBox1.SelectedItem).DisplayString + ": " + message + Environment.NewLine);
                    }
                    catch (CommunicationException)
                    {
                        MessageBox.Show("Соединение с пользователем " + ((PeerEntry)listBox1.SelectedItem).DisplayString + " было потеряно!", "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }
            }
        }
        //получение переданного файла
        internal void DisplayMessageByte(byte[] message, string from)
        {

            PeerEntry peerEntry = listBox1.SelectedItem as PeerEntry;
            if (peerEntry != null && peerEntry.ServiceProxy != null)
            {
                try
                {
                    label1.Text = "Получение файла... ";
                    byte[] fileLength = new byte[8];
                    byte[] pocketLength = new byte[4];
                    byte[] CRCd = new byte[4];
                    byte[] CRCs = new byte[4];
                    Buffer.BlockCopy(message, 9, pocketLength, 0, 4);

                    if (processing == 0)
                    {
                        Buffer.BlockCopy(message, 1, fileLength, 0, 8);
                        cData = new byte[BitConverter.ToInt64(fileLength, 0)];
                    }
                    progressBar1.Visible = true;
                    progressBar1.Maximum = progressBar1.Maximum;
                    if (progressBar1.Value + 63980 > progressBar1.Maximum)
                    {
                        progressBar1.Value = progressBar1.Maximum;
                    }
                    else
                    {
                        progressBar1.Value = progressBar1.Value + 63980;
                    }
                    byte[] temp = new byte[BitConverter.ToInt32(pocketLength, 0)];
                    Buffer.BlockCopy(message, 19, temp, 0, BitConverter.ToInt32(pocketLength, 0));
                    Buffer.BlockCopy(message, 15, CRCs, 0, 4);
                    CRCd = BitConverter.GetBytes((int)CS(temp));
                    int s = BitConverter.ToInt32(CRCs, 0);
                    int d = BitConverter.ToInt32(CRCd, 0);

                    if (s == d)
                    {
                        int a = BitConverter.ToInt32(pocketLength, 0);
                        Buffer.BlockCopy(message, 19, cData, dataLength, BitConverter.ToInt32(pocketLength, 0));
                        dataLength += BitConverter.ToInt32(pocketLength, 0);
                        processing = 1;

                        if (message[63999] == 84)
                        {
                            if (MessageBox.Show("Принять файл от " + ((PeerEntry)listBox1.SelectedItem).DisplayString + "?", "Получение файла", MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question) == DialogResult.Yes)
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
                                filename = saveFileDialog1.FileName;
                                //Разархивирование и расшифровка полученного файла
                                label1.Text = "Разархивирование... ";
                                if (compression == 1)
                                {

                                    File.WriteAllBytes("temp.rar", cData);
                                    ProcessStartInfo psi = new ProcessStartInfo();

                                    //Имя запускаемого приложения
                                    psi.FileName = "cmd";
                                    psi.WindowStyle = ProcessWindowStyle.Hidden;

                                    //команда, которую надо выполнить       
                                    psi.Arguments = "/с " + "\"" + @"C:\Program Files\WinRAR\Rar.exe" + "\"" + " e temp.rar";
                                    Process.Start(psi);

                                }
                                label1.Text = "Расшифровка.. ";
                                if (encription == 1)
                                {

                                    AES encryptor = new AES();
                                    encryptor.SetMultTable();
                                    //в зависимости от длины ключа(в байтах) устанавливаем параметры Nr и Nk
                                    if (key.Length == 16)
                                    {
                                        encryptor.SetParameters(10, 4);
                                    }
                                    if (key.Length == 24)
                                    {
                                        encryptor.SetParameters(12, 6);
                                    }
                                    if (key.Length == 32)
                                    {
                                        encryptor.SetParameters(14, 8);
                                    }

                                    //расшифровываем полученный текст
                                    cData = encryptor.AES_decrypt(cData, key);
                                }
                                File.WriteAllBytes(filename, cData);
                                dataLength = 0;
                                processing = 0;
                                progressBar1.Value = 0;
                                textBox2.AppendText(((PeerEntry)listBox1.SelectedItem).DisplayString + " отправил вам файл." + Environment.NewLine);
                                progressBar1.Visible = false;
                                label1.Text = "";

                            }
                            else
                            {
                                textBox2.AppendText("Вы отклонили файл от "+((PeerEntry)listBox1.SelectedItem).DisplayString + " ." + Environment.NewLine);
                                label1.Text = "";
                            }

                        }
                    }



                    progressBar1.Visible = false;
                }
                catch (CommunicationException)
                {
                    MessageBox.Show("Соединение с пользователем "+ ((PeerEntry)listBox1.SelectedItem).DisplayString+" было потеряно!", "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                    
                }
            }
            else
            {
                MessageBox.Show("Connection with current user lost!");
            }
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
            start = 1;
        }
        //разбивает упаковывает загруженный файл в пакеты и пересылает их указанному пользователю
        private void SendFile()
        {
            PeerEntry peerEntry = listBox1.SelectedItem as PeerEntry;
            if (peerEntry != null && peerEntry.ServiceProxy != null)
            {
                try
                {
                    
                    if (bData != null)
                    {
                        label1.Text = "Передача: " + filename;
                        byte[] fileLength = new byte[8];
                        byte[] pocketLength = new byte[4];
                        byte[] CRC = new byte[4];
                        //установка прогресс бара                        
                        progressBar1.Visible = true;
                        progressBar1.Value = 0;
                        progressBar1.Maximum = bData.Length;


                        ///////////////////////////////////////////////////////////////////////////////////////////
                        //|стартовый байт|длина файла|длина данных пакета|шифрование|сжатие|CRC|данные|конечный байт|//
                        ///////////////////////////////////////////////////////////////////////////////////////////

                        if (bData.Length <= 63980) //Размер файла меньше максимаьного размера блока данных пакета
                        {
                            byte[] message = new byte[64000];
                            fileLength = BitConverter.GetBytes((long)bData.Length);
                            pocketLength = BitConverter.GetBytes(bData.Length);
                            byte[] temp = new byte[bData.Length];
                            Buffer.BlockCopy(bData, 0, temp, 0, bData.Length);
                            CRC = BitConverter.GetBytes((int)CS(temp));
                            message[0] = 170;                                   //Стартовый байт
                            Buffer.BlockCopy(fileLength, 0, message, 1, 8);     //Длина файла
                            Buffer.BlockCopy(pocketLength, 0, message, 9, 4);   //Длина данных пакета
                            message[13] = encription;                           //Шифрование
                            message[14] = compression;                          //Сжатие
                            Buffer.BlockCopy(CRC, 0, message, 15, 4);          //Контрольная сумма
                            message[63999] = 84;                                //Конечный байт

                            Buffer.BlockCopy(bData, 0, message, 19, bData.Length);
                            peerEntry.ServiceProxy.SendMessageByte(message, username);
                            bData = null;
                            label1.Text = "";


                            progressBar1.Value = progressBar1.Maximum;

                        }
                        else //Размер файла больше максимального размера блока данных пакета
                        {
                            byte[] nolik = { 0, 0, 0, 0 };
                            fileLength = BitConverter.GetBytes((long)bData.Length);
                            pocketLength = BitConverter.GetBytes(63980);
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
                            for (int i = 0; i < bData.Length / 63980 + 1; i++)
                            {

                                if (length - 63980 >= 0)
                                {
                                    byte[] temp = new byte[63980];
                                    Buffer.BlockCopy(bData, count, temp, 0, 63980);
                                    CRC = BitConverter.GetBytes((int)CS(temp));
                                    Buffer.BlockCopy(CRC, 0, message, 15, 4);

                                    pocketLength = BitConverter.GetBytes(63980);
                                    Buffer.BlockCopy(pocketLength, 0, message, 9, 4);
                                    Buffer.BlockCopy(bData, count, message, 19, 63980);
                                    if (length == 63980)
                                    {
                                        message[63999] = 84;
                                    }
                                    peerEntry.ServiceProxy.SendMessageByte(message, username);
                                    Buffer.BlockCopy(nolik, 0, message, 9, 4);
                                    count += 63980;
                                    length -= 63980;
                                    if (progressBar1.Value + 63980 > progressBar1.Maximum)
                                    {
                                        progressBar1.Value = progressBar1.Maximum;
                                    }
                                    else
                                    {
                                        progressBar1.Value = progressBar1.Value + 63980;
                                    }
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
                                    byte[] temp = new byte[length];
                                    Buffer.BlockCopy(bData, count, temp, 0, length);
                                    CRC = BitConverter.GetBytes((int)CS(temp));
                                    Buffer.BlockCopy(CRC, 0, message, 15, 4);

                                    Buffer.BlockCopy(bData, count, message, 19, length);
                                    peerEntry.ServiceProxy.SendMessageByte(message, username);

                                    if (progressBar1.Value + 63980 > progressBar1.Maximum)
                                    {
                                        progressBar1.Value = progressBar1.Maximum;
                                    }
                                    else
                                    {
                                        progressBar1.Value = progressBar1.Value + 63980;
                                    }
                                }
                            }
                            bData = null;
                            label1.Text = "";
                        }
                    }
                    else
                    {
                        peerEntry.ServiceProxy.SendMessage(textBox1.Text, username);
                        textBox2.AppendText(username + ": " + textBox1.Text + Environment.NewLine);
                        textBox1.Text = "";
                    }
                }
                catch (CommunicationException)
                {
                    MessageBox.Show("Соединение было разорвано!", "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Остановка регистрации
                    peerNameRegistration.Stop();
                    Hide();
                    // Остановка WCF-сервиса
                    host.Close();
                    AuthForm u = new AuthForm();
                    u.Show();
                    //ConErrorHandler();
                }
            }
        }
        private void FileSending(object sender, EventArgs e)
        {
            SendFile();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        // Загрузка файла в массив bData
        private void LoadbData(object sender, EventArgs e)
        {

            bData = LoadFIle();
            if (bData != null)
            {
                label1.Text = "Сжатие: " + filename;
                if (compression == 1)
                {
                    byte[] temp = null;
                    temp = bData;
                    File.WriteAllBytes("temp.tmp", temp);
                    ProcessStartInfo psi = new ProcessStartInfo();

                    //Имя запускаемого приложения
                    psi.FileName = "cmd";
                    psi.WindowStyle = ProcessWindowStyle.Hidden;

                    //команда, которую надо выполнить       
                    psi.Arguments = "/с " + "\"" + @"C:\Program Files\WinRAR\Rar.exe" + "\"" + " a temp.rar temp.tmp";
                    Process.Start(psi);
                    File.WriteAllBytes("temp.rar", temp);
                    bData = File.ReadAllBytes("temp.rar");
                }
                label1.Text = "Шифрование: " + filename;
                if (encription == 1)
                {
                    AES encryptor = new AES();
                    //Генерируем ключ и и формируем таблицу умножения в поле Галуа GF(256)                
                    encryptor.SetMultTable();
                    //в зависимости от длины ключа(в байтах устанавливаем параметры Nr и Nk
                    if (key.Length == 16)
                    {
                        encryptor.SetParameters(10, 4);
                    }
                    if (key.Length == 24)
                    {
                        encryptor.SetParameters(12, 6);
                    }
                    if (key.Length == 32)
                    {
                        encryptor.SetParameters(14, 8);
                    }
                    //шифрование
                    byte[] temp = bData;
                    byte[] text;
                    //дополняем массив текст нулевыми символами, если количество байт в исходнодном тексте не кратно 16
                    int size = temp.Length;
                    if (temp.Length % 16 != 0)
                    {
                        while (size % 16 != 0)
                        {
                            size++;
                        }
                        text = new byte[size];
                        for (int i = 0; i < temp.Length; i++)
                            text[i] = temp[i];
                        for (int i = temp.Length; i < size; i++)
                            text[i] = 0;
                    }
                    else
                    {
                        text = temp;
                    }
                    bData = encryptor.AES_encrypt(text, key);
                }

                label1.Text = filename;

            }
            
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Остановка регистрации
                peerNameRegistration.Stop();

                // Остановка WCF-сервиса
                host.Close();
                Application.Exit();
            }
            catch
            {
                Application.Exit();
            }
            
        }

        private void параметрыПередачиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }
        //загрузка данных из файла
        public byte[] LoadFIle ()
        {
            byte[] data=null;
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
                    if (((myStream = openFileDialog1.OpenFile()) != null) && (Path.GetExtension(openFileDialog1.SafeFileName) != ".exe"))
                    {
                        using (myStream)
                        {
                            data = File.ReadAllBytes(openFileDialog1.FileName);
                            filename = openFileDialog1.SafeFileName;
                        }
                        return data;
                    }
                    else
                        if (Path.GetExtension(openFileDialog1.SafeFileName) == ".exe")
                        {
                            MessageBox.Show("You trying to load an executable file. Please, NO!");
                        }
                    return null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                    return null;
                }
            }
            return data;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            { 
                PeerEntry peerEntry = listBox1.SelectedItem as PeerEntry;
                try
                {
                    peerEntry.ServiceProxy.SendMessage(0x00 + "peertest" + 0xff, username);
                }
                catch (CommunicationException)
                {
                    textBox2.AppendText("Попытка установить соединение с " + ((PeerEntry)listBox1.SelectedItem).DisplayString + "...." + Environment.NewLine);
                }
            }
            catch
            {

            }
        }
               

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                SendFile();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendFile();
                textBox1.Clear();

            }

        }
    }
}
