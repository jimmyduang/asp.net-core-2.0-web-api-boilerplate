﻿using System;
using Infrastructure.Features.Common;

namespace SalesApi.ViewModels.Subscription.Order
{
    public class SubscriptionOrderViewModel: EntityBase
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int MilkmanId { get; set; }
        public int SubscriptionProductSnapshotId { get; set; }
        public int? SubscriptionMonthPromotionId { get; set; }
        public int PresetDayCount { get; set; }
        public int PresetDayGift { get; set; } // 批条

        public DateTime? PaidTime { get; set; }
    }
}
