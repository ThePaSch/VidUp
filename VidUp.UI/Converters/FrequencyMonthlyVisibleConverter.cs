﻿using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.Converters
{
    public class FrequencyMonthlyVisibleConverter : MarkupExtension, IValueConverter
    {
        private static FrequencyMonthlyVisibleConverter converter;

        public FrequencyMonthlyVisibleConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ScheduleFrequency)
            {
                ScheduleFrequency scheduleFrequency = (ScheduleFrequency)value;
                if (scheduleFrequency == ScheduleFrequency.Monthly)
                {
                    return "Visible";
                }

                return "Collapsed";
            }

            throw new ArgumentException("Value to convert is not a ScheduleFrequency.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new FrequencyMonthlyVisibleConverter());
        }
    }
}