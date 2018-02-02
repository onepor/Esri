﻿// Copyright 2018 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific
// language governing permissions and limitations under the License.

using ArcGISRuntime.Samples.Managers;
using Esri.ArcGISRuntime.LocalServices;
using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ArcGISRuntime.WPF.Samples.DynamicWorkspaceRaster
{
    public partial class DynamicWorkspaceRaster
    {
        // Hold a reference to the local map service
        private LocalMapService _localMapService;

        // Hold a reference to the layer that will display the Raster data
        private ArcGISMapImageSublayer _rasterSublayer;

        public DynamicWorkspaceRaster()
        {
            InitializeComponent();

            // Create the UI, setup the control references and execute initialization
            Initialize();
        }

        private async void Initialize()
        {
            // Create a map and add it to the view
            MyMapView.Map = new Map(Basemap.CreateNavigationVector());

            try
            {
                // Start the local server instance
                await LocalServer.Instance.StartAsync();

                // Load the sample data
                await LoadRasterPaths();

                // Enable the 'choose Raster' button
                MyChooseButton.IsEnabled = true;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(String.Format("Please ensure that local server is installed prior to using the sample. See instructions in readme.me or metadata.json. Message: {0}", ex.Message));
            }
        }

        private async void StartLocalMapService(string filename, string path)
        {
            // Start a service from the blank MPK
            String mapServiceUrl = await GetMpkPath();

            // Create the local map service
            _localMapService = new LocalMapService(mapServiceUrl);

            // Create the Raster workspace; this workspace name was chosen arbitrarily
            RasterWorkspace rasterWorkspace = new RasterWorkspace("ras_wkspc", path);

            // Create the layer source that represents the Raster on disk
            RasterSublayerSource source = new RasterSublayerSource(rasterWorkspace.Id, filename);

            // Create a sublayer instance from the table source
            _rasterSublayer = new ArcGISMapImageSublayer(0, source);

            // Add the dynamic workspace to the map service
            _localMapService.SetDynamicWorkspaces(new List<DynamicWorkspace>() { rasterWorkspace });

            // Subscribe to notifications about service status changes
            _localMapService.StatusChanged += _localMapService_StatusChanged;

            // Start the map service
            await _localMapService.StartAsync();
        }

        private async void _localMapService_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            // Return if the service hasn't started yet
            if (e.Status != LocalServerStatus.Started) return;

            // Create the imagery layer
            ArcGISMapImageLayer imageryLayer = new ArcGISMapImageLayer(_localMapService.Url);

            // Subscribe to image layer load status change events
            // Only set up the sublayer renderer for the Raster after the parent layer has finished loading
            imageryLayer.LoadStatusChanged += (q, ex) =>
            {
                // Add the layer to the map once loaded
                if (ex.Status == Esri.ArcGISRuntime.LoadStatus.Loaded)
                {
                    // Add the Raster sublayer to the imagery layer
                    imageryLayer.Sublayers.Add(_rasterSublayer);
                }
            };

            // Load the layer
            await imageryLayer.LoadAsync();

            // Clear any existing layers
            MyMapView.Map.OperationalLayers.Clear();

            // Add the image layer to the map
            MyMapView.Map.OperationalLayers.Add(imageryLayer);
        }

        private async Task<string> GetMpkPath()
        {
            // Gets the path to the blank map package

            #region offlinedata

            // The data manager provides a method to get the folder
            string folder = DataManager.GetDataFolder();

            // Get the full path
            string filepath = Path.Combine(folder, "SampleData", "DynamicWorkspaceRaster", "mpk_blank.mpk");

            // Check if the file exists
            if (!File.Exists(filepath))
            {
                // Download the file
                await DataManager.GetData("ea619b4f0f8f4d108c5b87e90c1b5be0", "DynamicWorkspaceRaster");
            }

            return filepath;

            #endregion offlinedata
        }

        private async Task<String> LoadRasterPaths()
        {
            // Gets the path to the Raster package

            #region offlinedata

            // The data manager provides a method to get the folder
            string folder = DataManager.GetDataFolder();

            // Get the full path
            string filepath = Path.Combine(folder, "SampleData", "DynamicWorkspaceRaster", "usa_raster.tif");

            // Check if the file exists
            if (!File.Exists(filepath))
            {
                // Download the file
                await DataManager.GetData("80b43ba48f524a8eb0cb54f0f1ee9a5f", "DynamicWorkspaceRaster");
            }

            return filepath;

            #endregion offlinedata
        }

        private async void MyChooseButton_Click(object sender, RoutedEventArgs e)
        {
            // Allow the user to specify a file path - create the dialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog()
            {
                DefaultExt = ".tif",
                Filter = "Rasters|*.tif",
                InitialDirectory = Path.GetDirectoryName(await LoadRasterPaths()) ?? "C:\\"
            };

            // Show the dialog and get the results
            bool? result = dlg.ShowDialog();

            // Take action if the user selected a file
            if (result == true)
            {
                string filename = Path.GetFileName(dlg.FileName);
                string path = Path.GetDirectoryName(dlg.FileName);
                StartLocalMapService(filename, path);
            }
        }
    }
}