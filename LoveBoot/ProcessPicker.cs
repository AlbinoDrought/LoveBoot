using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace LoveBoot
{
    public partial class ProcessPicker : Form
    {
        public string PickedProcessName = null;

        public ProcessPicker()
        {
            InitializeComponent();
        }

        private void ProcessPicker_Load(object sender, EventArgs e)
        {
            Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach(Process p in processes)
            {
                cbProcess.Items.Add(p.ProcessName);
            }
            cbProcess.Sorted = true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            object selectedItem = cbProcess.SelectedItem;
            
            /*if(selectedItem.GetType() == typeof(Process))
            {
                this.PickedProcessName = ((Process)selectedItem).ProcessName.Replace(".exe", "");
            }
            else
            {*/
                // user-entered string
                this.PickedProcessName = selectedItem.ToString();
            //}

            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
