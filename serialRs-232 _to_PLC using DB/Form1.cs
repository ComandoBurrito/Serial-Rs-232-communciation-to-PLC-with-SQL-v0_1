using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibplctagWrapper; //Com PC to PLC & viceversa
using System.IO.Ports; //Com Cognex Scanner to PC via RS-232 port
using System.IO; //Archivos Configurables.txt
using System.Data.Sql; //Com con db
using System.Threading; //Timers    

namespace serialRs_232__to_PLC_using_DB
{
    public partial class Form1 : Form
    {

        //Variables - PC<>PLC
        private const int DataTimeout = 5000;                                               // variable que determina el tiempo máximo que se permite para la execucion de lectura de datos
        private Tag tagRead1, tagWrite1, tagWriteModel;
        private string readAddress1 = "B3:0/0", writeAddress1 = "B3:0/1", writeModel = "N7:50";  //Direcciones Int16

        private SerialPort serialPort1; // Declaracion del puerto serial como objeto
        //private System.IO.Ports.SerialPort serialPort1;

        string PlcIP = "";
        int nCOM = 0;
        string PathDataOutput = "";
        string SerialPCB = "";

        public Form1()
        {
            InitializeComponent();

            // Crear tags en el constructor del formulario
            tagRead1 = new Tag("192.168.1.1", CpuType.SLC, readAddress1, DataType.Int16, 1);
            tagWrite1 = new Tag("192.168.1.1", CpuType.SLC, writeAddress1, DataType.Int16, 1);
            tagWriteModel = new Tag("192.168.1.1", CpuType.SLC, writeModel, DataType.Int16, 10);

            // Inicializa la instancia del SerialPort y configura el ComboBox con los puertos disponibles.
            //CargarPuertosDisponibles();
            //CargarBaudios();
            //serialPort = new SerialPort();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);


            // Asociar el evento DataReceived al método serialPort_DataReceived
            //serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);

            // serialPort1
            // 
            this.serialPort1.BaudRate = 115200;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TimerMain.Enabled = true;
            btnRead1.Enabled = false;
            btnWrite1.Enabled = false;
        }

        public bool Read_Setup_File(string thefile = "")
        {
            try
            {
                var MainPath = Directory.GetCurrentDirectory();
                string LimitPath = Path.Combine(MainPath, "Limits"); // Use Path.Combine for path concatenation

                if (string.IsNullOrEmpty(thefile))
                {
                    thefile = Path.Combine(MainPath, "CONFIG.txt");
                }

                using (var sr = new StreamReader(thefile))
                {
                    string line;
                    bool bTodoOk = true;

                    while ((line = sr.ReadLine()) != null) // Simplify the loop condition
                    {
                        if (IsCommentOrEmpty(line))
                        {
                            continue;
                        }

                        bTodoOk = ProcLines(line.Trim());

                        if (!bTodoOk)
                        {
                            MessageBox.Show("Error occurred during the reading of the CONFIG.txt file");
                            return false;
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false;
            }
        }

        private bool IsCommentOrEmpty(string line)
        {
            return string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith("'");
        }

        private bool ProcLines(string Argumento)
        {
            string[] LineArgs;
            //string[] Com_Args;

            string Cmd1 = "";
            string Cmd2 = "";
            bool bRet = false;

            if (Argumento.Contains("="))
            {
                LineArgs = Argumento.Split('=');
                Cmd1 = LineArgs[0].ToUpper().Trim();
                Cmd2 = LineArgs[1].ToUpper().Trim();
            }
            else
            {
                return bRet;
            }

            try
            {
                switch (Cmd1)
                {



                    case "COM_NUMBER":
                        {

                            nCOM = Convert.ToInt32(Cmd2);

                            break;
                        }

                    case "PLC_IP":
                        {
                            PlcIP = Cmd2.Trim();
                            break;
                        }

                    
                    case "PATH_DATAOUTPUT":
                        {
                            PathDataOutput = Cmd2.Trim();
                            break;
                        }

                }

                bRet = true;
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrio un Error en Linea de Archivo Config.txt {ex.Message}\r\n");
            }

            return bRet;
        }

        public void LoadControls()
        {
            tbCOM.Text = Convert.ToString(nCOM);
            tb_IP.Text = PlcIP;
        }

        // Resto del codigo


 /*       
                private void btnConnect_port_Click(object sender, EventArgs e)
                {
                    if (cbPorts.SelectedItem != null && cbBaudios.SelectedItem != null)
                    {
                        try
                        {
                            serialPort.PortName = cbPorts.SelectedItem.ToString();
                            serialPort.BaudRate = (int)cbBaudios.SelectedItem;
                            serialPort.Open();

                            // Verificar si el puerto se ha abierto correctamente
                            if (serialPort.IsOpen)
                            {
                                MessageBox.Show("Conexión establecida correctamente.", "Conexión Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                // Muestra el mensaje en un TextBox llamado txtStatus
                                tbStatus.Text = "Conexión establecida correctamente.";
                                tbStatus.ForeColor = Color.Green;
                            }
                            else
                            {
                                MessageBox.Show("Error al conectar: No se pudo abrir el puerto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                // Muestra un mensaje de error en el TextBox
                                tbStatus.Text = "Error al conectar: No se pudo abrir el puerto.";
                                tbStatus.ForeColor = Color.Red;
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Error al conectar: " + "'PortName' no se puede establecer mientras el puerto está abierto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //ex.Message
                            // Muestra un mensaje de error en el TextBox
                            tbStatus.Text = "Error al conectar: " + "'PortName' no se puede establecer mientras el puerto está abierto."; //ex.Message
                            tbStatus.ForeColor = Color.Red;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Selecciona un puerto y velocidad de baudios antes de conectar.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // Muestra un mensaje de advertencia en el TextBox
                        tbStatus.Text = "Selecciona un puerto y velocidad de baudios antes de conectar.";
                        tbStatus.ForeColor = Color.Orange;
                    }
                }  //HABILITAR PARA PRUEBA CON COMBOBOX
  */      
        private void btnDisconnect_port_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
        } //HABILITAR PARA PRUEBA CON COMBOBOX

        private void btnSendTrigger_Click(object sender, EventArgs e)
        {
            //lectura
            SerialPCB = "";
            SerialPCB = TriggerScan();
            

            if (SerialPCB == null)
            {
                    tb_PCBSerial.Text = SerialPCB;   
            }
            else
            {
                tbStatus.Text = "Error al escanear: " + "'Codigo Data Matrix' no se ha podido obtener. Revise que el codigo se encuentre bien posicionado"; //Muestra un mensaje de error en el TextBox
                tbStatus.ForeColor = Color.Orange;
                MessageBox.Show("Error al escanear: " + "'Codigo Data Matrix' no se ha podido obtener. Revise que el codigo se encuentre bien posicionado", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning); //ex.Message                                                                                                                                    
            }


        }
        
        private void TimerMain_Tick(object sender, EventArgs e)
        {
            TimerMain.Enabled = false;

            //Aqui leo Config.txt
            Read_Setup_File();
            LoadControls();
        }

        public string TriggerScan()
        {
            string sRet = "";

            DateTime Ini_ = DateTime.Now.AddSeconds(5);
            DateTime OldTime = DateTime.Now;


            serialPort1.PortName = "COM" + nCOM;
            string TriggerStr = "+";

            if (this.serialPort1.IsOpen != true)
            {
                this.serialPort1.Open();
                // Verificar si el puerto se ha abierto correctamente
                if (serialPort1.IsOpen)
                {
                    // Muestra el mensaje en un TextBox llamado txtStatus
                    tbStatus.Text = "Conexión establecida correctamente.";
                    tbStatus.ForeColor = Color.Green;
                    MessageBox.Show("Conexión establecida correctamente.", "Conexión Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Muestra un mensaje de error en el TextBox
                    tbStatus.Text = "Error al conectar: No se pudo abrir el puerto.";
                    tbStatus.ForeColor = Color.Red;
                    MessageBox.Show("Error al conectar: No se pudo abrir el puerto.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Thread.Sleep(15);
            }

            do
            {
                serialPort1.Write(TriggerStr);
                Thread.Sleep(500);
                sRet = serialPort1.ReadExisting();



            } while (sRet == "" & (DateTime.Now < Ini_));
            this.serialPort1.Close();
            sRet = sRet.Trim();
            return sRet;
        }

        /*PLC COM SECTION*/

        private void btnWrite1_Click(object sender, EventArgs e)
        {
                using (var client = new Libplctag())
                {
                try
                {
                    //Se añade el tag cada que se vaya a utilizar la variable cliente
                    client.AddTag(tagWrite1); //tags de escritura1

                    // Verifica que el tag haya sido añadido, si devuelve PENDING hay que reintentarlo
                    while (client.GetStatus(tagWrite1) == Libplctag.PLCTAG_STATUS_PENDING)
                    {
                        Thread.Sleep(500);
                    }

                    // Si el status de conexión no está OK, tenemos que tratar el error
                    if (client.GetStatus(tagWrite1) != Libplctag.PLCTAG_STATUS_OK)
                    {
                        textBoxConsole.Invoke((Action)(() =>
                        {
                            textBoxConsole.Text = $"Error al configurar el estado interno de la etiqueta. Error: {client.DecodeError(client.GetStatus(tagRead1))}\r\n";
                        }));
                        return;
                    }


                    //****************************************************************



                    var sendValueText = textBoxWriteData.Text; //valor a mandar al PLC

                    //  =   =   ESCRITURA DE TAGS =   =
                    // establece valores en el búfer de tags de escritura
                    //OBLIGATORIO: se debe utilizar "SetInt16Value" para mandar ya que el PLC solo puede recibir valores en 16bits

                    if (short.TryParse(sendValueText, out short sendValue))
                    {
                        // Ahora 'sendValue' contiene el valor 'short' convertido
                        // Usa 'sendValue' en tu llamada al método
                        client.SetInt16Value(tagWrite1, 0 * tagWrite1.ElementSize, sendValue); // escribe un Int16, sea variable o constante (suma)
                    }
                    else
                    {
                        // Handle the case where the text cannot be converted to a short
                        // Display an error message or take appropriate action
                        MessageBox.Show("Invalid input. Please enter a valid short value.");
                    }

                    // escribe los valores (variable de status)
                    var resultWrite = client.WriteTag(tagWrite1, DataTimeout);      //Declaracion de variables de resultado en la escritura

                    // comnprueba el resultado
                    if (resultWrite != Libplctag.PLCTAG_STATUS_OK)
                    {
                        textBoxConsole.Invoke((Action)(() =>
                        {
                            textBoxConsole.Text = $"ERROR: No se pueden leer los datos. Código de error {resultWrite}: {client.DecodeError(resultWrite)}\n";
                        }));
                        return;
                    }
                    //Imprime los valores escritos

                    textBoxConsole.Invoke((Action)(() =>
                    {
                        
                        textBoxConsole.Text = writeAddress1 + " <> " + sendValue + "\r\n";
                    }));
                    //return;       //habilitado solo en prueba
                }
                catch (Exception ex)
                {
                    textBoxConsole.Invoke((Action)(() =>
                    {
                        textBoxConsole.Text = $"Se produjo una excepción: {ex.Message}\r\n";
                    }));
                    // Puedes manejar la excepción de manera específica aquí si es necesario
                }

            }
        }

        

        private void btnRead1_Click(object sender, EventArgs e)
        {
            using (var client = new Libplctag())
            {
                try
                {
                    //Se añade el tag cada que se vaya a utilizar la variable cliente
                    client.AddTag(tagRead1); //tags de lectura1

                    // Verifica que el tag haya sido añadido, si devuelve PENDING hay que reintentarlo
                    while (client.GetStatus(tagRead1) == Libplctag.PLCTAG_STATUS_PENDING)
                    {
                        Thread.Sleep(500);
                    }

                    // Si el status de conexión no está OK, tenemos que tratar el error
                    if (client.GetStatus(tagRead1) != Libplctag.PLCTAG_STATUS_OK)
                    {
                        textBoxConsole.Invoke((Action)(() =>
                        {
                            textBoxConsole.Text = $"Error al configurar el estado interno de la etiqueta. Error: {client.DecodeError(client.GetStatus(tagRead1))}\r\n";
                        }));
                        return;
                    }

                    // ********************************************************************************************************************

                    //  =   =   LECTURA DE TAGS =   =
                    // Ejecuta la lectura
                    var result1 = client.ReadTag(tagRead1, DataTimeout);        //Declaracion de variables de resultado en la lectura
                                                                                //var result2 = client.ReadTag(tagRead2, DataTimeout);
                                                                                // Comprobacion del resultado de la operación de lectura
                    if (result1 != Libplctag.PLCTAG_STATUS_OK)
                    {
                        textBoxConsole.Invoke((Action)(() =>
                        {
                            textBoxConsole.Text = $"ERROR: No se pueden leer los datos. Código de error {client.DecodeError(client.GetStatus(tagRead1))}\r\n";
                        }));
                    }
                    Thread.Sleep(500);
                    // Conversion de datos
                    var realResult = client.GetInt16Value(tagRead1, 0 * tagRead1.ElementSize);              //multiplica con tag.ElementSize para mantener los índices consistentes con los índices en el plc
                                                                                                            //var N7_90 = client.GetInt16Value(tagWrite1, 0 * tagRead2.ElementSize);

                    textBoxReadData.Invoke((Action)(() =>
                    {
                        textBoxReadData.Text = readAddress1 + " <> " + realResult + "\r\n";
                    }));
                    textBoxConsole.Invoke((Action)(() =>
                    {
                        textBoxConsole.Text = $"Éxito al capturar el dato interno de la etiqueta. Status: {client.DecodeError(client.GetStatus(tagRead1))}\r\n";
                    }));
                    //return;       //habilitado solo en prueba
                }
                catch (Exception ex)
                {
                    textBoxConsole.Invoke((Action)(() =>
                    {
                        textBoxConsole.Text = $"Se produjo una excepción: {ex.Message}\r\n";
                    }));
                    // Puedes manejar la excepción de manera específica aquí si es necesario
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                using (var client = new Libplctag())
                {
                    try
                    {
                        // añade el tag
                        client.AddTag(tagRead1); //tags de lectura1
                        client.AddTag(tagWrite1); //tag de escxritura1
                        client.AddTag(tagWriteModel); //tags de escrituraDeModelo1

                        try
                        {
                            // ... tu código existente

                            // Manejar el tag de lectura
                            int maxRetries = 5;
                            int retryCount = 0;

                            while (client.GetStatus(tagRead1) == Libplctag.PLCTAG_STATUS_PENDING && retryCount < maxRetries)
                            {
                                Thread.Sleep(500);
                                retryCount++;
                            }

                            if (retryCount >= maxRetries)
                            {   
                                textBoxConsole.AppendText("Error: No se pudo obtener el estado del tag de lectura después de varios intentos.\r\n");
                                throw new Exception("Error: No se pudo obtener el estado del tag de lectura después de varios intentos.");
                            }

                            // Manejar el tag de escritura
                            retryCount = 0;

                            while (client.GetStatus(tagWrite1) == Libplctag.PLCTAG_STATUS_PENDING && retryCount < maxRetries)
                            {
                                Thread.Sleep(500);
                                retryCount++;
                            }

                            if (retryCount >= maxRetries)
                            {
                                textBoxConsole.AppendText("Error: No se pudo obtener el estado del tag de escritura después de varios intentos.\r\n");
                                throw new Exception("Error: No se pudo obtener el estado del tag de escritura después de varios intentos.");
                            }

                            // ... tu código existente
                        }
                        catch (Exception ex)
                        {
                            // Maneja la excepción aquí, muestra el mensaje en el TextBox y deshabilita los botones de lectura y escritura
                            btnRead1.Enabled = false;
                            btnWrite1.Enabled = false;
                            textBoxConsole.AppendText($"Error: {ex.Message}\r\n");
                        }


                        // Si el estatus de conexión está OK, el PLC se habrá enlazado exitosamente
                        // NOTA: esto aplica para cada variable
                        if (client.GetStatus(tagRead1) == Libplctag.PLCTAG_STATUS_OK && client.GetStatus(tagWrite1) == Libplctag.PLCTAG_STATUS_OK)
                        {
                            textBoxConsole.Invoke((Action)(() =>
                            {
                                btnRead1.Enabled = true;
                                btnWrite1.Enabled = true;
                                textBoxConsole.Text = $"Éxito al entablar comunicación con el PLC. Status: {client.DecodeError(client.GetStatus(tagRead1))}\r\n";
                            }));
                            //return;       //habilitado solo en prueba
                        }

                        // Si el status de conexión no está OK, tenemos que tratar el error
                        if (client.GetStatus(tagRead1) != Libplctag.PLCTAG_STATUS_OK || client.GetStatus(tagWrite1) != Libplctag.PLCTAG_STATUS_OK)
                        {
                            textBoxConsole.Invoke((Action)(() =>
                            {
                                textBoxConsole.Text = $"Error al entablar comunicación con el PLC. Error: {client.DecodeError(client.GetStatus(tagRead1))}\r\n";
                            }));
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        textBoxConsole.Invoke((Action)(() =>
                        {
                            textBoxConsole.Text = $"Se produjo una excepción: {ex.Message}\r\n";
                        }));
                        // Puedes manejar la excepción de manera específica aquí si es necesario
                    }

                    // Asegúrate de cambiar "textBoxConsole" al nombre correcto de tu TextBox en el formulario
                    //Action<string> writeToConsole = message => textBoxConsole.AppendText(message + Environment.NewLine);
                }
            }
            finally
            {
                // Console.ReadKey() no es necesario en una aplicación Windows Forms
            }
        }

    }
}
