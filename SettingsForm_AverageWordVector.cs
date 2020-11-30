using System.IO;
using System.Text;
using System.Windows.Forms;
using System;

namespace AverageWordVector
{
    internal partial class SettingsForm_AverageWordVector : Form
    {


        #region Get and Set Options

        public string InputFileName { get; set; }
        public string SelectedEncoding { get; set; }
        public int VectorSize { get; set; }
        public int VocabSize { get; set; }


       #endregion



        public SettingsForm_AverageWordVector(string InputFile, string SelectedEncodingIncoming, int VecSize, int VocSize)
        {
            InitializeComponent();

            foreach (var encoding in Encoding.GetEncodings())
            {
                EncodingDropdown.Items.Add(encoding.Name);
            }

            try
            {
                EncodingDropdown.SelectedIndex = EncodingDropdown.FindStringExact(SelectedEncodingIncoming);
            }
            catch
            {
                EncodingDropdown.SelectedIndex = EncodingDropdown.FindStringExact(Encoding.Default.BodyName);
            }

            VocabSize = VocSize;
            VectorSize = VecSize;
            SelectedFileTextbox.Text = InputFile;

            if (VocabSize == -1) ModelDetailsTextbox.Text = "Vocab size: unknown; Vector Size: " + VectorSize.ToString();
            else ModelDetailsTextbox.Text = "Vocab size: " + VocabSize.ToString() + "; Vector Size: " + VectorSize.ToString();


        }






        private void SetFolderButton_Click(object sender, System.EventArgs e)
        {

            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;
                dialog.Title = "Please choose the model file that you would like to read";
                dialog.FileName = "Model.txt";
                dialog.Filter = "Word Embedding Model (.txt,.vec)|*.txt;*.vec";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    


                    try
                    {
                        using (var stream = File.OpenRead(dialog.FileName))
                        using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(EncodingDropdown.SelectedItem.ToString())))
                        {

                            string[] firstLine = reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if(firstLine.Length == 2)
                            {
                                VocabSize = int.Parse(firstLine[0]);
                                VectorSize = int.Parse(firstLine[1]);
                                ModelDetailsTextbox.Text = "Vocab size: " + firstLine[0] + "; Vector Size: " + firstLine[1];
                            }
                            else
                            {
                                VectorSize = firstLine.Length - 1;
                                VocabSize = -1;
                                ModelDetailsTextbox.Text = "Vocab size: unknown; Vector Size: " + VectorSize.ToString();
                            }

                            

                            
                            SelectedFileTextbox.Text = dialog.FileName;

                        }

                    }
                    catch
                    {
                        MessageBox.Show("There was an error while trying to read your word embedding model. It is possible that your file is not correctly formatted, or that your model file is open in another program.", "Error reading model", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }


                }
            }


        }


        private void OKButton_Click(object sender, System.EventArgs e)
        {
            this.SelectedEncoding = EncodingDropdown.SelectedItem.ToString();
            this.InputFileName = SelectedFileTextbox.Text;
            this.DialogResult = DialogResult.OK;
        }


    }
}
