using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace P2P
{
    public partial class AuthForm : Form
    {
        public AuthForm()
        {
            InitializeComponent();          


        }
        MainForm mf = new MainForm();
        //При возникновении события ConErrorHandler вновь отображаем окно авторизации
        public void OnConnectionError()
        {            
            Show();
        }
        //Заходим в главное окно клиента
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {

                //Подписываемся на событие ConErrorHandler
                MainForm.ConErrorHandler += OnConnectionError;
                mf.Owner = this;                
                mf.username = textBox1.Text;
                mf.portnumber = textBox2.Text;
                if (Convert.ToInt32(textBox2.Text) > 1024 && Convert.ToInt32(textBox2.Text) < 65535)
                {
                    if (checkBox2.Checked)
                    {
                        mf.compression = 1;
                    }
                    if (checkBox1.Checked)
                    {
                        mf.encription = 1;
                        if (radioButton1.Checked)
                        {
                            mf.key=mf.LoadFIle();
                            if (mf.key.Length > 32)
                            {
                                MessageBox.Show("Неверная длина ключа в указанном файле " + mf.filename + "!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        if (radioButton2.Checked)
                        {
                            mf.key = AES_GenerateKey();

                        }
                    }
                    else
                    {
                        mf.encription = 0;
                    }


                    Hide();
                    mf.Show();
                    
                }
                else
                {
                    MessageBox.Show("Номер порта должен находится в диапазоне от 1024 до 65535!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Ошибка!",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        //Создаёт случайный ключ для алгоритма AES
        static byte[] AES_GenerateKey()
        {
            byte [] key = new byte[32];
            Random a = new Random();
            for (int i = 0; i < 32; i++)
            {
                key[i] = (byte)a.Next(0, 255);
            }
            return key;      

        }
        //отключаем ненужные radiobutton при изменении состояния checkBox1
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked )
            {
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                radioButton3.Enabled = false;
                radioButton3.Checked = false;
            }
            else
            {
               
                radioButton1.Enabled = false;
                radioButton2.Enabled = false;
                radioButton3.Enabled = true;
                radioButton3.Checked = true;
            }
        }
    }
}
