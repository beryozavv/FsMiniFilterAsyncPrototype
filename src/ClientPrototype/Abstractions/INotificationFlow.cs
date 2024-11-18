﻿using ClientPrototype.Dto;

namespace ClientPrototype.Abstractions;

public interface INotificationFlow
{
    Task PostAsync(RequestNotification request);
    void Complete();
}
