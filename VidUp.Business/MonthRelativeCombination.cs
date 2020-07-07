﻿using System;
using System.CodeDom;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class MonthRelativeCombination
    {
        [JsonProperty]
        private DayPosition dayPosition;
        [JsonProperty]
        private DayOfWeek dayOfWeek;

        public DayPosition DayPosition
        {
            get => this.dayPosition;
            set => this.dayPosition = value;
        }

        public DayOfWeek DayOfWeek
        {
            get => this.dayOfWeek;
            set => this.dayOfWeek = value;
        }

        public MonthRelativeCombination(DayPosition dayPosition, DayOfWeek dayOfWeek)
        {
            this.dayPosition = dayPosition;
            this.dayOfWeek = dayOfWeek;
        }
    }
}
