using System.Collections;
using UnityEngine;
using System;

#if UNITY_ANDROID 
using Unity.Notifications.Android;
#endif // UNITY_ANDROID

#if UNITY_IOS 
using Unity.Notifications.iOS;
#endif // UNITY_IOS

public class NotificationManager : MonoBehaviour
{
    private const string ChannelId = "channel_id";
    private const int NotificationDayInterval = 1;

#if UNITY_ANDROID 
    void Start()
    {
        // RequestNotificationPermission();
        // CreateNotificationChannel();    
        // ReScheduleNotification();   
    }

    void CreateNotificationChannel()
    {
        AndroidNotificationChannel channel = new AndroidNotificationChannel()
        {
            Id = ChannelId,
            Name = "Default Channel",
            Importance = Importance.Default,
            Description = "Generic notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    /// <summary>
    /// Schedules a repeated notification intended to prompt the user at different intervals.<br/>
    /// 
    /// This method is called when the app is first launched.    
    /// </summary>
    /// <remarks>
    /// The method first ensures that any previously scheduled notifications are canceled to prevent duplicates. 
    /// Then, it proceeds to schedule new notifications with specified delays.<br/>
    /// 
    /// The notifications are intended to encourage the user to spend time on a language learning
    /// activity. The text content of the notification is currently in Finnish, aiming to motivate
    /// the user to practice Finnish vocabulary on the CaptainA platform.
    /// </remarks>
    public void ReScheduleNotification()
    {    
        // Need to cancel all notifications before scheduling new ones
        AndroidNotificationCenter.CancelAllNotifications();
        
        AndroidNotification notification = new AndroidNotification()
        {
            Title = "Opiskellaan Suomea!",
            Text = "Käytä 10 minuuttia suomen sanastoon CaptainA:lla. Voit aloittaa nyt.",
            SmallIcon = "icon_nof_s",
            LargeIcon = "icon_nof_l",
            FireTime = System.DateTime.Now.ToLocalTime().AddDays(NotificationDayInterval),
            RepeatInterval = new TimeSpan(NotificationDayInterval, 0, 0, 0),            
            ShouldAutoCancel = true       
        };

        // Debug.Log("Sending notification with time: " + notification.FireTime.ToString());
        // Debug.Log("Sending notification with time: " + notification.ToString());

        AndroidNotificationCenter.SendNotification(notification, ChannelId);
    }

    IEnumerator RequestNotificationPermission()
    {
        var request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
            yield return null;        
        // here use request.Status to determine users response
    }

#endif // UNITY_ANDROID


#if UNITY_IOS
    void Start()
    {
        RequestNotificationPermission();
        // ReScheduleNotification();
    }

    void ReScheduleNotification()
    {
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
        iOSNotificationCenter.RemoveAllScheduledNotifications();

        iOSNotificationTimeIntervalTrigger timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(24, 0, 0),
            Repeats = true
        };

        iOSNotification notification = new iOSNotification()
        {
            // You can specify a custom identifier which can be used to manage the notification later.
            // If you don't provide one, a unique string will be generated automatically.
            Identifier = "_notification_01",
            Title = "Opiskellaan Suomea!",
            Subtitle = "Voit aloittaa nyt.",
            Body = "Käytä 10 minuuttia suomen sanastoon CaptainA:lla.",            
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
    }

    IEnumerator RequestNotificationPermission()
    {
        AuthorizationOption authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
        using (AuthorizationRequest req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            };

            string res = "\n RequestAuthorization:";
            res += "\n finished: " + req.IsFinished;
            res += "\n granted :  " + req.Granted;
            res += "\n error:  " + req.Error;
            res += "\n deviceToken:  " + req.DeviceToken;
            Debug.Log(res);
        }
    }

#endif // UNITY_IOS
}