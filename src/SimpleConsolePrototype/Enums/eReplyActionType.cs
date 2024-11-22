namespace SimpleConsolePrototype.Enums;

/// <summary>
/// Тип ответа для драйвера. Указание что делать с файлом.
/// </summary>
public enum eReplyActionType
{
    /// <summary> 
    /// Запретить открытие.
    /// </summary>
    Deny = 0,

    /// <summary> 
    /// Перенаправить открытие на другой файл.
    /// </summary>
    Redirect = (1 << 0),

    /// <summary> 
    /// Разрешён просмотр документа.
    /// </summary>
    Open = (1 << 1),

    /// <summary> 
    /// Печать документа разрешена.
    /// </summary>
    Print = (1 << 2),

    /// <summary> 
    /// Разрешено сохранение файла через функцию «Сохранить как».
    /// </summary>
    SaveAs = (1 << 3),

    /// <summary> 
    /// Разрешено редактирование и сохранение документа.
    /// </summary>
    Edit = (1 << 4),

    /// <summary> 
    /// Копирование содержимого в буфер обмена разрешено.
    /// </summary>
    CopyContent = (1 << 5),

    /// <summary> 
    /// Разрешить всё.
    /// </summary>
    Allow = Deny | Open | Print | SaveAs | Edit | CopyContent,
}

