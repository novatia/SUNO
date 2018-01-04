using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace SUNO
{
    public partial class Utils : Form
    {
        List<TrajectorySet> tss = new List<TrajectorySet>();

        public Utils()
        {
            InitializeComponent();
            AddEventHandlers();
            Utils_Load();
        }

        public void AddEventHandlers()
        {
            Hough_relativeIntensityScrollBar.ValueChanged   += new EventHandler(this.Hough_relativeIntensityScrollBar_ValueChanged);

            Susan_geometricalTresholdScrollBar.ValueChanged += new EventHandler(this.Susan_geometricalTresholdScrollBar_ValueChanged);
            Susan_differenceTresholdScrollBar.ValueChanged  += new EventHandler(this.Susan_differenceTresholdScrollBar_ValueChanged);

            Suno_lineScanRadialScrollBar.ValueChanged += new EventHandler(this.Suno_lineScanRadialScrollBar_ValueChanged);
            Suno_lineScanThresholdScrollBar.ValueChanged += new EventHandler(this.Suno_lineScanThresholdScrollBar_ValueChanged);
            Suno_radialThresholdScrollBar.ValueChanged += new EventHandler(this.Suno_radialThresholdScrollBar_ValueChanged);
        }

        public void Suno_lineScanRadialScrollBar_ValueChanged(Object sender, EventArgs e)
        {
            Suno_lineScanRadialTextBox.Text = "" + Suno_lineScanRadialScrollBar.Value;
        }

        public void Suno_lineScanThresholdScrollBar_ValueChanged(Object sender, EventArgs e)
        {
            Suno_lineScanThresholdTextBox.Text = "" + Suno_lineScanThresholdScrollBar.Value;
        }

        public void Suno_radialThresholdScrollBar_ValueChanged(Object sender, EventArgs e)
        {
            Suno_radialThresholdTextbox.Text = "" + Suno_radialThresholdScrollBar.Value;
        }

        public void Hough_relativeIntensityScrollBar_ValueChanged(Object sender, EventArgs e)
        {
            Decimal value = Decimal.Parse(""+Hough_relativeIntensityScrollBar.Value) / 1000;
            Hough_relativeIntensityTextBox.Text = ""+value;
        }

        public void Susan_geometricalTresholdScrollBar_ValueChanged(Object sender, EventArgs e)
        {
            Susan_geometricalTresholdTextBox.Text = "" + Susan_geometricalTresholdScrollBar.Value;
        }

        public void Susan_differenceTresholdScrollBar_ValueChanged(Object sender, EventArgs e)
        {
            Susan_differenceTresholdTextBox.Text = "" + Susan_differenceTresholdScrollBar.Value;
        }

     

        private void fileChooserButton_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                try
                {
                    image_path.Text = file;
                }
                catch (IOException)
                {

                }
            }
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            logic.SunoLogic suno = new logic.SunoLogic();

            if (image_path.Text.Equals(""))
                return;

            Bitmap tempImage = (Bitmap)Bitmap.FromFile(image_path.Text);

            TrajectorySet ts = suno.CalculateTrajectorySet(tempImage, Double.Parse(Hough_relativeIntensityTextBox.Text), Int32.Parse(Susan_differenceTresholdTextBox.Text), Int32.Parse(Susan_geometricalTresholdTextBox.Text), Int32.Parse(Suno_lineScanThresholdTextBox.Text), Int32.Parse(Suno_lineScanRadialTextBox.Text), Int32.Parse(Suno_radialThresholdTextbox.Text), geometricalCheckBox.Checked);

            tempImage = suno.renderSunoPreview(ts, Int32.Parse(cornerRadiusTextbox.Text), Int32.Parse(lineWidthTextbox.Text), renderSusanCornerCheckbox.Checked, renderHoughLinesCheckbox.Checked, renderBlackPointsCheckbox.Checked, renderTrajectoryPointsCheckbox.Checked);
            previewPictureBox.Image =  tempImage ;

            //populate stats
            blackSegmentsLabel.Text = "" + ts.black_segments.Count;
            elaborationTimeLabel.Text = "" + ((ts.elaboration_end_time.Ticks - ts.elaboration_start_time.Ticks)/10000000) + " s";
            trajectoryPointsLabel.Text = "" + ts.trajectory_points.Count;
        }

        private void scanButton_Click(object sender, EventArgs e)
        {
            devicesList.Items.Clear();
            devicesList.Items.AddRange(logic.CameraLogic.ListDevices());
        }

        private void TakePictureButton_Click(object sender, EventArgs e)
        {
            model.WIACamera camera = (model.WIACamera)devicesList.SelectedItem;

            //logic.CameraLogic.InitCamera(camera, ISOComboBox.SelectedItem.ToString(), exposureTextBox,Text);
          
            Image image = logic.CameraLogic.TakePicture(camera);

            previewPictureBox.Image = image;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            model.WIACamera camera = (model.WIACamera)devicesList.SelectedItem;

            cameraInfoTextBox.Text=logic.CameraLogic.getInfo(camera);
           
        }

        private void geometricalCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (geometricalCheckBox.Checked)
            {
                Suno_radialThresholdScrollBar.Enabled = false;
                Suno_radialThresholdTextbox.Enabled = false;
            }
            else {
                Suno_radialThresholdScrollBar.Enabled = true;
                Suno_radialThresholdTextbox.Enabled = true;
            }
        }

        private  void Utils_Load()
        {
            tss = logic.SunoLogic.DeserializeTrajectorySet();
            totalTrajLabel.Text = ""+tss.Count;
        }

        private void ISOComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void exposureTextBox_TextChanged(object sender, EventArgs e)
        {

        }
        
        
    }
}
