using System.Collections;
using UnityEngine;
using Unity.Notifications.Android;
using System;

public class NotificationManager : MonoBehaviour
{
    private const string ChannelId = "channel_id";
    private const int NotificationDayInterval = 1;

    void Start()
    {
        RequestNotificationPermission();
        CreateNotificationChannel();     
        ReScheduleNotification();   
    }

#if UNITY_ANDROID    
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
        AndroidNotification notification = new AndroidNotification()
        {
            Title = "Opiskellaan Suomea!",
            Text = "Käytä 5 minuuttia suomen sanastoon CaptainA:lla. Voit aloittaa nyt.",
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


// #if UNITY_IOS
// using Unity.Notifications.iOS;
//
//     /// <summary>
//     /// Schedules a series of notifications intended to prompt the user at different intervals.
//     /// This method queues notifications to be sent after 24, 72, and 144 hours respectively.
//     /// Each notification carries a unique ID to distinguish it from others.<br/>
//     /// 
//     /// This method is called when the app is first launched, and also when the user
//     /// press on a flashcard deck.
//     /// </summary>
//     /// <remarks>
//     /// The method first ensures that any previously scheduled notifications with the same IDs
//     /// are canceled to prevent duplicates. Then, it proceeds to schedule new notifications
//     /// with specified delays.<br/>
//     /// 
//     /// The notifications are intended to encourage the user to spend time on a language learning
//     /// activity. The text content of the notification is currently in Finnish, aiming to motivate
//     /// the user to practice Finnish vocabulary on the CaptainA platform.
//     /// </remarks>
//     public void ReScheduleNotifications()
//     {
//         CancelScheduledNotifications();

//         SendNotification(24, NotificationId24h);
//         SendNotification(72, NotificationId72h);
//         SendNotification(144, NotificationId144h);
//     }

//     void SendNotification(int hours, int notificationId)
//     {
//         iOSNotificationTimeIntervalTrigger timeTrigger = new iOSNotificationTimeIntervalTrigger();
//         {
//             TimeInterval = new TimeSpan(0, minutes, seconds),
//             Repeats = false
//         };   
        
//         var notification = new iOSNotification()
//         {
//             // You can specify a custom identifier which can be used to manage the notification later.
//             // If you don't provide one, a unique string will be generated automatically.
//             Identifier = "_notification_01",
//             Title = "Opiskellaan Suomea!",
//             Body = "Käytä 5 minuuttia suomen sanastoon CaptainA:lla. Voit aloittaa nyt.",
//             Subtitle = "Käytä 5 minuuttia suomen sanastoon CaptainA:lla. Voit aloittaa nyt.",
//             ShowInForeground = true,
//             ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
//             CategoryIdentifier = "category_a",
//             ThreadIdentifier = "thread1",
//             Trigger = timeTrigger,
//         };

//         iOSNotificationCenter.ScheduleNotification(notification);
//     }

//     void CancelScheduledNotifications()
//     {
//         AndroidNotificationCenter.CancelNotification(NotificationId24h);
//         AndroidNotificationCenter.CancelNotification(NotificationId72h);
//         AndroidNotificationCenter.CancelNotification(NotificationId144h);
//     }

//     IEnumerator RequestNotificationPermission()
//     {
//         AuthorizationOption authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
//         using (AuthorizationRequest req = new AuthorizationRequest(authorizationOption, true))
//         {
//             while (!req.IsFinished)
//             {
//                 yield return null;
//             };

//             string res = "\n RequestAuthorization:";
//             res += "\n finished: " + req.IsFinished;
//             res += "\n granted :  " + req.Granted;
//             res += "\n error:  " + req.Error;
//             res += "\n deviceToken:  " + req.DeviceToken;
//             Debug.Log(res);
//         }
//     }

// #endif // UNITY_IOS

}