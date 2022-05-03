﻿using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Analytical.Windows
{
    public static partial class Query
    {
        public static bool TryGetWeatherData(out WeatherData weatherData, IWin32Window owner = null)
        {
            weatherData = null;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "epw files (*.epw)|*.epw|TAS TBD files (*.tbd)|*.tbd|TAS TSD files (*.tsd)|*.tsd|TAS TWD files (*.twd)|*.twd|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog(owner) != DialogResult.OK)
                {
                    return false;
                }

                string path_WeatherData = openFileDialog.FileName;
                string extension = System.IO.Path.GetExtension(path_WeatherData).ToLower().Trim();
                if (string.IsNullOrWhiteSpace(extension))
                {
                    return false;
                }

                try
                {
                    if (extension.EndsWith("epw"))
                    {
                        weatherData = Weather.Convert.ToSAM(path_WeatherData);
                    }
                    else
                    {
                        List<WeatherData> weatherDatas = Weather.Tas.Convert.ToSAM_WeatherDatas(path_WeatherData);
                        if (weatherDatas == null || weatherDatas.Count == 0)
                        {
                            return false;
                        }

                        if (weatherDatas.Count == 1)
                        {
                            weatherData = weatherDatas[0];
                        }
                        else
                        {
                            weatherDatas.Sort((x, y) => x.Name.CompareTo(y.Name));

                            using (Core.Windows.Forms.ComboBoxForm<WeatherData> comboBoxForm = new Core.Windows.Forms.ComboBoxForm<WeatherData>("Select Weather Data", weatherDatas, (WeatherData x) => x.Name))
                            {
                                if (comboBoxForm.ShowDialog(owner) != DialogResult.OK)
                                {
                                    return false;
                                }

                                weatherData = comboBoxForm.SelectedItem;
                            }

                        }

                    }
                }
                catch (Exception exception)
                {
                    weatherData = null;
                }
            }

            return weatherData != null;
        }
    }
}