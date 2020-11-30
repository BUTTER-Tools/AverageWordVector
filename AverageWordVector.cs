using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using PluginContracts;
using OutputHelperLib;
using System.IO;


namespace AverageWordVector
{
    public class AverageWordVector : Plugin
    {


        public string[] InputType { get; } = { "Tokens" };
        public string OutputType { get; } = "OutputArray";

        public Dictionary<int, string> OutputHeaderData { get; set; } = new Dictionary<int, string>() { { 0, "Tokens" },
                                                                                                        { 1, "TokensCaptured" } };
        public bool InheritHeader { get; } = false;

        #region Plugin Details and Info

        public string PluginName { get; } = "Average Word Vector";
        public string PluginType { get; } = "Language Analysis";
        public string PluginVersion { get; } = "1.0.0";
        public string PluginAuthor { get; } = "Ryan L. Boyd (ryan@ryanboyd.io)";
        public string PluginDescription { get; } = "Using a pre-trained word embedding model, this plugin will calculate the average vector for each text."  + Environment.NewLine + Environment.NewLine +
            "Note that this analysis is case sensitive, and this method cannot score texts for words that do not exist in your pre-trained model.";
        public bool TopLevel { get; } = false;
        public string PluginTutorial { get; } = "https://youtu.be/ocVnWg2N5tY";

        private double[][] model { get; set; }
        private int TotalNumRows { get; set; }
        private bool modelHasHeader { get; set; }


        public Icon GetPluginIcon
        {
            get
            {
                return Properties.Resources.icon;
            }
        }

        #endregion



        private string IncomingTextLocation { get; set; } = "";
        private string SelectedEncoding { get; set; } = "utf-8";
        private int VocabSize { get; set; } = 0;
        private int VectorSize { get; set; } = 0;
        private Dictionary<string, int> WordToArrayMap { get; set; }


        public void ChangeSettings()
        {

            using (var form = new SettingsForm_AverageWordVector(IncomingTextLocation, SelectedEncoding, VectorSize, VocabSize))
            {


                form.Icon = Properties.Resources.icon;
                form.Text = PluginName;


                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    SelectedEncoding = form.SelectedEncoding;
                    IncomingTextLocation = form.InputFileName;
                    VocabSize = form.VocabSize;
                    VectorSize = form.VectorSize;
                }
            }

        }




        //not used
        public Payload RunPlugin(Payload Input)
        {
            Payload pData = new Payload();
            pData.FileID = Input.FileID;
            pData.SegmentID = Input.SegmentID;

            for (int i = 0; i < Input.StringArrayList.Count; i++)
            {

                string[] OutputArray = new string[VectorSize + 2];
                for (int j = 0; j < OutputArray.Length; j++) OutputArray[j] = "";

                double[] textVector = new double[VectorSize];
                for (int j = 0; j < VectorSize; j++) textVector[j] = 0;

                int NumberOfDetectedWords = 0;

                //tally up an average vector for the text
                #region get mean text vector
                for(int tokenNumber = 0; tokenNumber < Input.StringArrayList[i].Length; tokenNumber++)
                {

                    if (WordToArrayMap.ContainsKey(Input.StringArrayList[i][tokenNumber]))
                    {
                        double[] detectedVec = model[WordToArrayMap[Input.StringArrayList[i][tokenNumber]]];
                        textVector = textVector.Zip(detectedVec, (x, y) => x + y).ToArray();
                        NumberOfDetectedWords++;
                    }

                }

                if (NumberOfDetectedWords > 0)
                {
                    for (int j = 0; j < VectorSize; j++) textVector[j] = textVector[j] / NumberOfDetectedWords;
                }
                #endregion


                OutputArray[0] = Input.StringArrayList[i].Length.ToString();
                OutputArray[1] = NumberOfDetectedWords.ToString();
                for (int j = 0; j < VectorSize; j++) OutputArray[j + 2] = textVector[j].ToString();

                pData.SegmentNumber.Add(Input.SegmentNumber[i]);
                pData.StringArrayList.Add(OutputArray);

            }

            return (pData);
        }





        public void Initialize()
        {

            OutputHeaderData = new Dictionary<int, string>() { { 0, "Tokens" },
                                                               { 1, "TokensCaptured" } }; ;
            TotalNumRows = 0;

            string leadingZeroes = "D" + VectorSize.ToString().Length.ToString();
            for (int i = 0; i < VectorSize; i++) OutputHeaderData.Add(i + 2, "v" + (i+1).ToString(leadingZeroes));




            //we could use a List<double[]> to load in the word vectors, then
            //just .ToArray() it to make jagged arrays. However, I *really* want to avoid
            //having to hold the model in memory twice
            WordToArrayMap = new Dictionary<string, int>();
            if (VocabSize != -1) model = new double[VocabSize][];

            try
            {

           



                #region capture dictionary words and initialize model, if vocabsize is known
                //now, during initialization, we actually go through and want to establish the word group vectors
                using (var stream = File.OpenRead(IncomingTextLocation))
                using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
                {

                    if (VocabSize != -1)
                    {
                        string[] firstLine = reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    int WordsFound = 0;

                    while (!reader.EndOfStream)
                    {

                    
                        string line = reader.ReadLine().TrimEnd();
                        string[] splitLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string RowWord = splitLine[0].Trim();
                        double[] RowVector = new double[VectorSize];
                        for (int i = 0; i < VectorSize; i++) RowVector[i] = Double.Parse(splitLine[i + 1]);

                        if (!WordToArrayMap.ContainsKey(RowWord))
                        {
                            WordToArrayMap.Add(RowWord, TotalNumRows);
                            if (VocabSize != -1) model[TotalNumRows] = RowVector;
                        }

                        TotalNumRows++;

                    }
                }


                #endregion



                //if we didn't know the vocab size initially, we know it now that we've walked the whole model
                #region if vocab size was unknown, now we load up the whole model into memory
                if (VocabSize == -1)
                {
                    model = new double[TotalNumRows][];
                    TotalNumRows = 0;

                    //now, during initialization, we actually go through and want to establish the word group vectors
                    using (var stream = File.OpenRead(IncomingTextLocation))
                    using (var reader = new StreamReader(stream, encoding: Encoding.GetEncoding(SelectedEncoding)))
                    {

                        while (!reader.EndOfStream)
                        {


                            string line = reader.ReadLine().TrimEnd();
                            string[] splitLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string RowWord = splitLine[0].Trim();
                            double[] RowVector = new double[VectorSize];
                            for (int i = 0; i < VectorSize; i++) RowVector[i] = Double.Parse(splitLine[i + 1]);

                            if (WordToArrayMap.ContainsKey(RowWord))
                            {
                                model[TotalNumRows] = RowVector;
                            }

                            TotalNumRows++;

                        }
                    }
                }
                    #endregion





            }
            catch (OutOfMemoryException OOM)
            {
                MessageBox.Show("Plugin Error: Distributed Dictionary. This plugin encountered an \"Out of Memory\" error while trying to load your pre-trained model. More than likely, you do not have enough RAM in your computer to hold this model in memory. Consider using a model with a smaller vocabulary or fewer dimensions.", "Plugin Error (Out of Memory)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }





public bool InspectSettings()
        {

            if (string.IsNullOrEmpty(IncomingTextLocation))
            {
                return false;
            }
            else
            {
                return true;
            }
            
        }



        public Payload FinishUp(Payload Input)
        {
            //wipe out the model so that garbage collect frees that RAM up
            Array.Clear(model, 0, model.Length);
            return (Input);
        }



        #region Import/Export Settings
        public void ImportSettings(Dictionary<string, string> SettingsDict)
        {
            SelectedEncoding = SettingsDict["SelectedEncoding"];
            IncomingTextLocation = SettingsDict["IncomingTextLocation"];
            VocabSize = int.Parse(SettingsDict["VocabSize"]);
            VectorSize = int.Parse(SettingsDict["VectorSize"]);
        }

        public Dictionary<string, string> ExportSettings(bool suppressWarnings)
        {
            Dictionary<string, string> SettingsDict = new Dictionary<string, string>();
            SettingsDict.Add("SelectedEncoding", SelectedEncoding);
            SettingsDict.Add("IncomingTextLocation", IncomingTextLocation);
            SettingsDict.Add("VocabSize", VocabSize.ToString());
            SettingsDict.Add("VectorSize", VectorSize.ToString());
            return (SettingsDict);
        }
        #endregion


    }
}
