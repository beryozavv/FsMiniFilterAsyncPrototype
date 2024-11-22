namespace SimpleConsolePrototype.Enums;

/// <summary>
/// Результат обработки уведомления от драйвера.
/// </summary>
public enum eProcessingResult
{
    /// <summary>
    /// Доступ разрешен.
    /// </summary>
    Allow = 1,

    /// <summary>
    /// Доступ запрещен.
    /// </summary>
    Deny = 2,

    /// <summary>
    /// Доступ по умолчанию.
    /// </summary>
    DefaultAction = 3,

    /// <summary>
    /// Приложение, пытающееся открыть файл не поддерживается.
    /// </summary>
    AppIsNotSupported = 4,

    /// <summary>
    /// Ошибка работы метода.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Приложение не находится в списке доверенных
    /// </summary>
    AppIsNotTrusted = 6
}

