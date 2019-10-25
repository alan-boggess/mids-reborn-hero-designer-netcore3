
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Hero_Designer.My;
using Import;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Hero_Designer
{
    public partial class frmImport_Powerset : Form
    {

        frmBusy bFrm;

        string FullFileName;

        PowersetData[] ImportBuffer;

        public frmImport_Powerset()
        {
            Load += frmImport_Powerset_Load;
            FullFileName = "";
            ImportBuffer = new PowersetData[0];
            InitializeComponent();
            ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(frmImport_Powerset));
            Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
            Name = nameof(frmImport_Powerset);
        }

        void btnCheckAll_Click(object sender, EventArgs e)

        {
            lstImport.BeginUpdate();
            int num = lstImport.Items.Count - 1;
            for (int index = 0; index <= num; ++index)
                lstImport.Items[index].Checked = true;
            lstImport.EndUpdate();
        }

        void btnClose_Click(object sender, EventArgs e)

        {
            Close();
        }

        void btnFile_Click(object sender, EventArgs e)

        {
            dlgBrowse.FileName = FullFileName;
            if (dlgBrowse.ShowDialog(this) == DialogResult.OK)
            {
                FullFileName = dlgBrowse.FileName;
                if (ParseClasses(FullFileName))
                    FillListView();
            }
            BusyHide();
            DisplayInfo();
        }

        void btnImport_Click(object sender, EventArgs e)

        {
            ProcessImport();
        }

        void btnUncheckAll_Click(object sender, EventArgs e)

        {
            lstImport.BeginUpdate();
            int num = lstImport.Items.Count - 1;
            for (int index = 0; index <= num; ++index)
                lstImport.Items[index].Checked = false;
            lstImport.EndUpdate();
        }

        void BusyHide()

        {
            if (bFrm == null)
                return;
            bFrm.Close();
            bFrm = null;
        }

        void BusyMsg(string sMessage)

        {
            if (bFrm == null)
            {
                bFrm = new frmBusy();
                bFrm.Show(this);
            }
            bFrm.SetMessage(sMessage);
        }

        public void DisplayInfo()
        {
            lblFile.Text = FileIO.StripPath(FullFileName);
            lblDate.Text = "Date: " + Strings.Format(DatabaseAPI.Database.PowersetVersion.RevisionDate, "dd/MMM/yy HH:mm:ss");
            udRevision.Value = new Decimal(DatabaseAPI.Database.PowersetVersion.Revision);
            lblCount.Text = "Records: " + Convert.ToString(DatabaseAPI.Database.Powersets.Length);
        }

        void FillListView()

        {
            string[] items = new string[5];
            lstImport.BeginUpdate();
            lstImport.Items.Clear();
            int num1 = 0;
            int num2 = ImportBuffer.Length - 1;
            for (int index = 0; index <= num2; ++index)
            {
                ++num1;
                if (num1 >= 100)
                {
                    BusyMsg(Strings.Format(index, "###,##0") + " records checked.");
                    num1 = 0;
                }

                if (!ImportBuffer[index].IsValid)
                    continue;
                items[0] = ImportBuffer[index].Data.FullName;
                items[1] = ImportBuffer[index].Data.GroupName;
                items[2] = !ImportBuffer[index].IsNew ? "No" : "Yes";
                bool flag = ImportBuffer[index].CheckDifference(out items[4]);
                items[3] = !flag ? "No" : "Yes";
                lstImport.Items.Add(new ListViewItem(items)
                {
                    Checked = flag,
                    Tag = index
                });
            }
            if (lstImport.Items.Count > 0)
                lstImport.Items[0].EnsureVisible();
            lstImport.EndUpdate();
        }

        void frmImport_Powerset_Load(object sender, EventArgs e)

        {
            FullFileName = DatabaseAPI.Database.PowersetVersion.SourceFile;
            DisplayInfo();
        }

        [DebuggerStepThrough]

        bool ParseClasses(string iFileName)

        {
            int num1 = 0;
            StreamReader iStream;
            try
            {
                iStream = new StreamReader(iFileName);
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                MessageBox.Show(ex.Message, "Powerset CSV Not Opened", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ProjectData.ClearProjectError();
                return false;
            }
            int num3 = 0;
            int num4 = 0;
            ImportBuffer = new PowersetData[0];
            int num5 = 0;
            try
            {
                string iString;
                do
                {
                    iString = FileIO.ReadLineUnlimited(iStream, char.MinValue);
                    if (iString == null || iString.StartsWith("#"))
                        continue;
                    ++num5;
                    if (num5 >= 100)
                    {
                        BusyMsg(Strings.Format(num3, "###,##0") + " records parsed.");
                        num5 = 0;
                    }
                    ImportBuffer = (PowersetData[])Utils.CopyArray(ImportBuffer, new PowersetData[ImportBuffer.Length + 1]);
                    ImportBuffer[ImportBuffer.Length - 1] = new PowersetData(iString);
                    ++num3;
                    if (ImportBuffer[ImportBuffer.Length - 1].IsValid)
                        ++num1;
                    else
                        ++num4;
                }
                while (iString != null);
            }
            catch (Exception ex)
            {
                ProjectData.SetProjectError(ex);
                Exception exception = ex;
                iStream.Close();
                MessageBox.Show(exception.Message, "Powerset Class CSV Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ProjectData.ClearProjectError();
                return false;
            }
            iStream.Close();
            MessageBox.Show("Parse Completed!\r\nTotal Records: " + Convert.ToString(num3) + "\r\nGood: " + Convert.ToString(num1) + "\r\nRejected: " + Convert.ToString(num4), "File Parsed");
            return true;
        }

        bool ProcessImport()

        {
            int num1 = 0;
            int num2 = lstImport.Items.Count - 1;
            for (int index = 0; index <= num2; ++index)
            {
                if (!lstImport.Items[index].Checked)
                    continue;
                ImportBuffer[Convert.ToInt32(lstImport.Items[index].Tag)].Apply();
                ++num1;
            }
            DatabaseAPI.Database.PowersetVersion.SourceFile = dlgBrowse.FileName;
            DatabaseAPI.Database.PowersetVersion.RevisionDate = DateTime.Now;
            DatabaseAPI.Database.PowersetVersion.Revision = Convert.ToInt32(udRevision.Value);
            DatabaseAPI.MatchAllIDs();
            var serializer = MyApplication.GetSerializer();
            DatabaseAPI.SaveMainDatabase(serializer);
            MessageBox.Show("Import of " + Convert.ToString(num1) + " records completed!", "Done");
            DisplayInfo();
            return false;
        }
    }
}