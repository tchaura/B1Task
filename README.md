# Приложение на WPF

## Функционал 1: Генерация и работа с txt-файлами

1. **Генерация файлов:**
    - Приложение генерирует 100 текстовых файлов, каждый из которых содержит 100 000 строк со следующей структурой:
        - Случайная дата за последние 5 лет
        - Случайный набор 10 латинских символов
        - Случайный набор 10 русских символов
        - Случайное положительное четное целочисленное число в диапазоне от 1 до 100 000 000
        - Случайное положительное число с 8 знаками после запятой в диапазоне от 1 до 20.

    Пример вывода:
    ```
    03.03.2015||ZAwRbpGUiK||мДМЮаНкуКД||14152932||7,87742021||
    23.01.2015||vgHKThbgrP||ЛДКХысХшЗЦ||35085588||8,49822372||
    17.10.2017||AuTVNvaGRB||мЧепрИецрА||34259646||17,7248118||
    24.09.2014||ArIAASwOnE||ЧпЙМдШлыфУ||23252734||14,6239438||
    16.10.2017||eUkiAhUWmZ||ЗэЖЫзЯШАэШ||27831190||8,10838026||
    ```

2. **Объединение файлов:**
    - Приложение объединяет сгенерированные файлы в один. При этом есть возможность удалить из всех файлов строки с заданным сочетанием символов, например, «abc». Приложение выводит информацию о количестве удаленных строк.

3. **Импорт данных в БД:**
    - Приложение импортирует данные из объединённого файла в базу данных. При импорте отображается ход процесса, включая количество импортированных строк и количество оставшихся строк.

## Функционал 2: Работа с Excel-файлами

1. **Перенос данных в СУБД:**
    - Приложение поддерживает загрузку данных из Excel-файлов оборотно-сальдовой ведомости (ОСВ) в СУБД.

2. **Отображение загруженных файлов:**
    - Приложение отображает список загруженных файлов с информацией о содержимом ОСВ.

3. **Отображение данных из СУБД:**
    - Приложение отображает ОСВ в формате таблицы в соответствии с выбранным загруженным файлом.
