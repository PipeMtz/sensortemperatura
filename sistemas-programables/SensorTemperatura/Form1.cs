using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace SensorTemperatura
{
    public partial class Form1 : Form
    {
        private bool motorEncendido = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Configuración inicial del puerto serie
                serialPort1.PortName = "COM3";
                serialPort1.BaudRate = 9600;
                serialPort1.Open();
                serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

                // Configuración del temporizador
                timer1.Interval = 500; // Reducir el intervalo a 500 milisegundos
                timer1.Start();

                // Inicializar DataGridView
                InitializeDataGridView();

                // Inicializar Chart
                InitializeChart();

                Log("Aplicación iniciada correctamente.");
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                Log("Error al iniciar la aplicación: " + error.Message);
            }
        }

        private void InitializeDataGridView()
        {
            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Hora";
            dataGridView1.Columns[1].Name = "Temperatura";
            dataGridView1.Columns[2].Name = "Estado Motor";
            dataGridView1.Columns[3].Name = "Peligro";
        }

        private void InitializeChart()
        {
            chart1.Series.Clear();
            var series = new Series
            {
                Name = "Temperature",
                Color = Color.Blue,
                ChartType = SeriesChartType.Line
            };
            chart1.Series.Add(series);
            chart1.ChartAreas[0].AxisX.LabelStyle.Enabled = false; // Deshabilitar etiquetas del eje X
            chart1.ChartAreas[0].AxisY.Title = "Temperature (C)";
            chart1.ChartAreas[0].AxisX.Title = ""; // No mostrar título en el eje X
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                timer1.Stop();
                serialPort1.Close();
                Log("Puerto serie cerrado.");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                Log("Error al cerrar el puerto serie: " + err.Message);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort1.ReadLine();
                Log("Datos recibidos: " + data);

                string[] parts = data.Split(',');

                if (parts.Length == 2)
                {
                    UpdateUI(parts[0], parts[1]);
                }
                else
                {
                    Log("Error: Los datos recibidos no tienen el formato esperado.");
                }
            }
            catch (Exception ex)
            {
                Log("Error en el manejo de datos recibidos: " + ex.Message);
            }
        }

        private void UpdateUI(string tempStr, string motorState)
        {
            if (lblTemperatura.InvokeRequired || lblMotor.InvokeRequired)
            {
                this.Invoke(new Action<string, string>(UpdateUI), new object[] { tempStr, motorState });
                return;
            }

            double temp = double.Parse(tempStr);
            lblTemperatura.Text = tempStr + " C";

            if (temp >= 33)
            {
                lblMotor.Text = "ON";
                lblMotor.BackColor = Color.Green;
                pictureBox1.Enabled = false;
                pictureBox2.Enabled = true;
            }
            else
            {
                lblMotor.Text = "OFF";
                lblMotor.BackColor = Color.Red;
                pictureBox1.Enabled = true;
                pictureBox2.Enabled = false;
            }

            Color dangerColor;
            if (temp <= 20)
            {
                dangerColor = Color.White;
            }
            else if (temp <= 30)
            {
                dangerColor = Color.Green;
            }
            else if (temp <= 40)
            {
                dangerColor = Color.Yellow;
            }
            else if (temp <= 50)
            {
                dangerColor = Color.Orange;
            }
            else
            {
                dangerColor = Color.Red;
            }
            lblTemperatura.BackColor = dangerColor;

            // Actualizar gráfico
            UpdateChart(tempStr);

            // Actualizar DataGridView
            UpdateDataGridView(temp, motorState);
        }

        private void UpdateChart(string tempStr)
        {
            if (chart1.InvokeRequired)
            {
                chart1.Invoke(new Action<string>(UpdateChart), new object[] { tempStr });
                return;
            }

            double temp;
            if (double.TryParse(tempStr, out temp))
            {
                var now = DateTime.Now;
                chart1.Series["Temperature"].Points.AddXY(now, temp);

                // Remover puntos viejos para mantener solo los últimos 10 segundos
                while (chart1.Series["Temperature"].Points.Count > 0 &&
                       chart1.Series["Temperature"].Points[0].XValue < (now - TimeSpan.FromSeconds(10)).ToOADate())
                {
                    chart1.Series["Temperature"].Points.RemoveAt(0);
                }

                chart1.ChartAreas[0].RecalculateAxesScale();
            }
        }

        private void UpdateDataGridView(double temp, string motorState)
        {
            string hourMinute = DateTime.Now.ToString("HH:mm tt");
            string tempStr = $"{temp:F1} C";
            string motorStatus = motorState == "ON" ? "ON" : "OFF";
            Color motorColor = motorState == "ON" ? Color.Green : Color.Red;

            string dangerLevel;
            Color dangerColor;
            if (temp <= 20)
            {
                dangerLevel = "";
                dangerColor = Color.White;
            }
            else if (temp <= 30)
            {
                dangerLevel = "Normal";
                dangerColor = Color.Green;
            }
            else if (temp <= 40)
            {
                dangerLevel = "Atención";
                dangerColor = Color.Yellow;
            }
            else if (temp <= 50)
            {
                dangerLevel = "Precaución";
                dangerColor = Color.Orange;
            }
            else
            {
                dangerLevel = "Peligro";
                dangerColor = Color.Red;
            }

            // Insertar fila al principio del DataGridView
            dataGridView1.Rows.Insert(0, hourMinute, tempStr, motorStatus, dangerLevel);
            dataGridView1.Rows[0].Cells[2].Style.BackColor = motorColor;
            dataGridView1.Rows[0].Cells[3].Style.BackColor = dangerColor;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                Log("Solicitando actualización al Arduino.");
                serialPort1.WriteLine("GET_DATA");
            }
            catch (Exception ex)
            {
                Log("Error al solicitar actualización al Arduino: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            if (txtLogs.InvokeRequired)
            {
                txtLogs.Invoke(new Action<string>(Log), new object[] { message });
            }
            else
            {
                txtLogs.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + message + Environment.NewLine);
            }
        }

        private void lblTemperatura_Click(object sender, EventArgs e)
        {
            // Método sin uso, se puede eliminar
        }

        private void lblMotor_Click(object sender, EventArgs e)
        {
            // Método sin uso, se puede eliminar
        }

        private void chart1_Click(object sender, EventArgs e)
        {
            // Método sin uso, se puede eliminar
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
