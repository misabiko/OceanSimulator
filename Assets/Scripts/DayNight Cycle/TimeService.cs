using UnityEngine;
using System;
using NUnit.Framework;

public class TimeService
{
   private readonly TimeSettings settings;
   private DateTime currentTime;
   private readonly TimeSpan sunriseTime;
   private readonly TimeSpan sunsetTime;

   public event Action OnSunrise = delegate { };
   public event Action OnSunset = delegate { };
   public event Action OnHourChange = delegate { };

   private readonly Observable<bool> isDayTime;
   private readonly Observable<int> currentHour;

   public DateTime CurrentTime => currentTime;
   
   public TimeService(TimeSettings settings)
   {
      this.settings = settings;
      currentTime = DateTime.Now.Date + TimeSpan.FromHours(settings.startHour);
      sunriseTime = TimeSpan.FromHours(settings.sunriseHour);
      sunsetTime = TimeSpan.FromHours(settings.sunsetHour);

      isDayTime = new Observable<bool>(IsDayTime());
      currentHour = new Observable<int>(currentTime.Hour);
      
      isDayTime.ValueChanged += day => (day ? OnSunrise : OnSunset)?.Invoke();
      currentHour.ValueChanged += _ => OnHourChange?.Invoke();
   }

   public void UpdateTime(float deltaTime)
   {
      currentTime = currentTime.AddSeconds(deltaTime * settings.timeMultiplier);
      isDayTime.Value = IsDayTime();
      currentHour.Value = currentTime.Hour;
   }

   public float CalculateSunAngle()
   {
      bool isDay = IsDayTime();
      float startDegree = isDay ? 0 : 180;
      TimeSpan start = isDay ? sunriseTime : sunsetTime;
      TimeSpan end = isDay ? sunsetTime : sunriseTime;

      TimeSpan totalTime = TimeDifference(start, end);
      TimeSpan elapsedtime = TimeDifference(start, currentTime.TimeOfDay);

      float percentage = (float) (elapsedtime.TotalMinutes / totalTime.TotalMinutes);
      
      return Mathf.Lerp(startDegree, startDegree + 180, percentage);
   }

   bool IsDayTime() => currentTime.TimeOfDay > sunriseTime && currentTime.TimeOfDay < sunsetTime;

   TimeSpan TimeDifference(TimeSpan from, TimeSpan to)
   {
      TimeSpan difference = to - from;
      return difference.TotalHours < 0 ? difference + TimeSpan.FromHours(24) : difference;
   }
}
