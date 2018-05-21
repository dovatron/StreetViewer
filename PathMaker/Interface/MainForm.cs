﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Net;

using PathFinder.StreetViewing.Service;
using PathFinder.Core;
using PathFinder.StreetViewing.JsonObjects.GoogleApiJson.Common;

using GMap.NET;
using GMap.NET.WindowsForms;
using PathFinder.StreetViewing;
using System.Drawing;
using CommonDetectorApi;

namespace PathFinder.Interface
{
    delegate void StringArgReturningVoidDelegate(string text);
    delegate void ButtonAvailabilityDelegate(Button button);
    delegate void BitmapArgReturningVoidDelegate(Bitmap image);

    public partial class MainForm : Form
    {
        private const string ERROR_MESSAGE = "Введены некорректные данные";
        private const string WEB_ERROR_MESSAGE = "Ошибка соединения";
        private const string STREET1_TOOLTIP_MESSAGE = "Введите начальную улицу";
        private const string STREET2_TOOLTIP_MESSAGE = "Введите конечную улицу";
        private const string RESULTLABEL_STREETVIEWS_DOWNLOADING = "Идет загрузка панорам";
        private const string RESULTLABEL_STREETVIEWS_SUCCESS = "Панорамы успешно загружены";

        private Controller controller;
        private Downloader downloader;
        private BypassService bypassService;
        private GMapForm gMapForm;
        private Parameters parameters;

        /// <summary>
        /// Стандартный конструктор.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            controller = new Controller();
            parameters = Parameters.Instance;
            SetToolTipProperties(toolTip1);
            detectorsListBox.Items.AddRange(controller.GetDetectorNamesList().ToArray());
            SetToolTipProperties(toolTip2);
            orderInput.Value = parameters.Order;
            radiusUpDown.Value = parameters.Radius;
            pluginPathTextBox.Text = parameters.PluginsPath;
        }

        // ==============================================================================================================
        // = Implementation
        // ==============================================================================================================

        // 
        // directionRequestButton events
        // 
        private void DirectionRequestButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (gMapForm.Markers.Count < 2 && (string.IsNullOrEmpty(startStreet.Text) || string.IsNullOrEmpty(endStreet.Text)))
                {
                    resultLabel.Text = ERROR_MESSAGE;
                }
                else
                {
                    List<PolylineChunk> directionChunks = new List<PolylineChunk>();
                    if (gMapForm.Markers.Count > 1 && gMapForm.Markers[0].IsVisible)
                    {
                        for (int i = 0; i < gMapForm.Markers.Count - 1; i++)
                        {
                            IList<PolylineChunk> partChunks = controller.GetDirection(gMapForm.Markers[i].Position, gMapForm.Markers[i + 1].Position);
                            directionChunks.AddRange(partChunks);
                        }
                    }
                    else if (!string.IsNullOrEmpty(startStreet.Text) && !string.IsNullOrEmpty(endStreet.Text))
                    {
                        KeyEventArgs keyEventArgs = new KeyEventArgs(Keys.Enter);
                        StartStreet_KeyUp(sender, keyEventArgs);
                        EndStreet_KeyUp(sender, keyEventArgs);
                        IList<PolylineChunk> partChunks = controller.GetDirection(startStreet.Text, endStreet.Text);
                        directionChunks.AddRange(partChunks);
                    }

                    gMapForm.DrawRoute(directionChunks);
                    if (downloader == null)
                    {
                        //this.streetViewsRequestButton.Enabled = true;
                    }

                }
            }
            catch (WebException ex)
            {
                resultLabel.Text = WEB_ERROR_MESSAGE;
            }
        }

        // 
        // allDirectionsButtonEvents
        //
        private void AllDirectionsButton_Click(object sender, EventArgs e)
        {
            if (gMapForm.Markers[0].IsVisible)
            {
                try
                {
                    List<PolylineChunk> areaChunks = controller.GetAllDirectionsOfArea(gMapForm.Markers[0].Position.Lat, gMapForm.Markers[0].Position.Lng);
                    gMapForm.DrawDbRoute(areaChunks);
                    if (gMapForm.AddedChunks.Count > 0)
                    {
                        //streetViewsRequestButton.Enabled = true;
                    }
                }
                catch (WebException we)
                {
                    resultLabel.Text = WEB_ERROR_MESSAGE;
                }
            }
        }

        // 
        // streetViewsRequestButton events
        //
        private void StreetViewsRequestButton_Click(object sender, EventArgs e)
        {
            List<List<Location>> list = new List<List<Location>>();
            streetVewsFolderDialog.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                string path = streetVewsFolderDialog.SelectedPath + "Chunks";
                downloader = controller.GetStreetViews(gMapForm.AddedChunks, path);
                Thread downloadStatusThread = new Thread(UpdateStatus);
                downloadStatusThread.Start();
                streetViewsRequestButton.Enabled = false;

            }
            catch (WebException ex)
            {
                resultLabel.Text = WEB_ERROR_MESSAGE;
            }
        }


        // 
        // settingsButton events
        //
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            if (orderInput.Value != 0)
            {
                parameters.Order = (int)orderInput.Value;
            }

            if (radiusUpDown.Value != 0)
            {
                parameters.Radius = (int)radiusUpDown.Value;
            }

            if (!String.IsNullOrEmpty(pluginPathTextBox.Text))
            {
                parameters.PluginsPath = pluginPathTextBox.Text;
                controller.ReloadDetectorManager();
                detectorsListBox.Items.Clear();
                detectorsListBox.Items.AddRange(controller.GetDetectorNamesList().ToArray());
            }
        }

        // 
        // gMap events
        // 
        private void GMap_Load(object sender, EventArgs e)
        {
            if (gMapForm == null)
            {
                gMapForm = new GMapForm(gMap, gMapMini);
                gMapForm.DrawDbRoute(LoadExistingChunks());
            }
        }

        private void gMapMini_Load(object sender, EventArgs e)
        {
            if (gMapForm == null)
            {
                gMapForm = new GMapForm(gMap, gMapMini);
                gMapForm.DrawDbRoute(LoadExistingChunks());
            }
        }

        private List<PolylineChunk> LoadExistingChunks()
        {
            return controller.LoadExistingChunks();
        }

        private void GMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            gMapForm.MouseDoubleClick(e);
        }

        private void gMapMini_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            gMapForm.MapMiniMouseDoubleClick(e);
        }

        private void GMap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            gMapForm.OnMarkerClick(item);
        }

        // 
        // startStreet TextBox events
        // 
        private void StartStreet_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(startStreet, STREET1_TOOLTIP_MESSAGE);
        }

        private void StartStreet_MouseLeave(object sender, EventArgs e)
        {
            toolTip1.Hide(startStreet);
        }

        private void StartStreet_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(startStreet.Text))
            {
                try
                {
                    Location geoCode = controller.GetGeocoding(startStreet.Text);

                    if (geoCode != null)
                    {
                        PointLatLng position = new PointLatLng(geoCode.Lat, geoCode.Lng);
                        gMapForm.AddMarkerByKeyUp(position, true);
                        this.streetViewsRequestButton.Enabled = false;
                        gMapForm.CalculateZoomAndPosition();
                    }
                    else
                    {
                        resultLabel.Text = ERROR_MESSAGE;
                    }
                    e.Handled = true;
                }
                catch (WebException ex)
                {
                    resultLabel.Text = WEB_ERROR_MESSAGE;
                }
            }
        }

        //
        // endStreet TextBox events
        //
        private void EndStreet_MouseHover(object sender, EventArgs e)
        {
            toolTip2.SetToolTip(endStreet, STREET2_TOOLTIP_MESSAGE);
        }

        private void EndStreet_MouseLeave(object sender, EventArgs e)
        {
            toolTip1.Hide(endStreet);
        }

        private void EndStreet_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(endStreet.Text))
            {
                try
                {
                    Location geoCode = controller.GetGeocoding(endStreet.Text);
                    if (geoCode != null)
                    {
                        PointLatLng position = new PointLatLng(geoCode.Lat, geoCode.Lng);
                        gMapForm.AddMarkerByKeyUp(position, false);
                        this.streetViewsRequestButton.Enabled = false;
                        gMapForm.CalculateZoomAndPosition();
                    }
                    else
                    {
                        resultLabel.Text = ERROR_MESSAGE;
                    }
                    e.Handled = true;
                }
                catch (WebException ex)
                {
                    resultLabel.Text = WEB_ERROR_MESSAGE;
                }
            }
        }

        // 
        // common
        //
        private void SetToolTipProperties(System.Windows.Forms.ToolTip toolTip)
        {
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
        }

        private void UpdateStatus()
        {
            while (downloader.Status < 100)
            {
                SetText("Загружено " + downloader.Status + "%");
                System.Threading.Thread.Sleep(1000);
                List<PolylineChunk> chunks = downloader.GetDownloadedChunks();
                gMapForm.ShiftToDbRoute(chunks);
            }

            this.SetText("Загрузка завершена");
            downloader = null;
            SetAvailability(streetViewsRequestButton);
        }

        private void SetText(string text)
        {
            if (resultLabel.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetText);
                Invoke(d, new object[] { text });
            }
            else
            {
                resultLabel.Text = text;
            }
        }

        private void SetStreetView(Bitmap image)
        {
            if (streetViewBox.InvokeRequired)
            {
                BitmapArgReturningVoidDelegate dlgt = new BitmapArgReturningVoidDelegate(SetStreetView);
                Invoke(dlgt, new object[] { image });
            }
            else
            {
                streetViewBox.Image = image;
            }

        }

        private void SetAvailability(Button button)
        {
            if (button.InvokeRequired)
            {
                ButtonAvailabilityDelegate d = new ButtonAvailabilityDelegate(SetAvailability);
                this.Invoke(d, new object[] { button });
            }
            else
            {
                button.Enabled = true;
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            gMapForm.ClearAddedRoute();
        }

        private void detectorsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            detecorsInfoListView.Items.Clear();
            foreach (string detectorName in GetCheckedDetectorsNamesList())
            {
                ListViewItem item = detecorsInfoListView.Items.Add(detectorName);
                item.SubItems.Add("0");
                item.SubItems.Add("Не найдено ни одного знака на данном изображении");

            }
            detecorsInfoListView.Refresh();
        }

        private List<string> GetCheckedDetectorsNamesList()
        {
            List<string> checkedDetectorsList = new List<string>();
            for (int i = 0; i < detectorsListBox.Items.Count; i++)
            {
                if (detectorsListBox.GetItemChecked(i))
                {
                    checkedDetectorsList.Add(detectorsListBox.Items[i].ToString());
                }
            }
            return checkedDetectorsList;
        }

        private void detectSignsInViewsButton_Click(object sender, EventArgs e)
        {
            try
            {
                bypassService = controller.StartDetection(gMapForm.GetDetectorsStartChunk(), GetCheckedDetectorsNamesList());
                Thread byPassStatusThread = new Thread(UpdateBypassResults);
                byPassStatusThread.Start();
                detectSignsInViewsButton.Enabled = false;
            }
            catch (WebException ex)
            {
                resultLabel.Text = WEB_ERROR_MESSAGE;
            }
        }

        private void UpdateBypassResults()
        {
            while (bypassService.Status == 1)
            {
                List<BypassResult> bypassResults = bypassService.GetBypassResults();
                List<PolylineChunk> chunks = bypassService.GetDownloadedChunks();
                gMapForm.ShiftToDetectedRoute(chunks);
                foreach (BypassResult result in bypassResults)
                {
                    SetStreetView(result.Image);
                    System.Threading.Thread.Sleep(1000);
                }
                System.Threading.Thread.Sleep(1000);
            }
            bypassService = null;
            SetAvailability(detectSignsInViewsButton);
        }
    }
}