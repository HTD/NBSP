# NBSP

## Inserts Unicode non-breaking space on Ctrl+Space global shortcut

### About

Did you think you can just insert non-breaking space in Windows using Ctrl+Space or similar key combination?
I did. But nope, Windows does not support it by default. Windows Notepad handles that Unicode character properly, however there is no quick and simple way to enter it with the keyboard. FTFY.

There is also Resource Editor in Visual Studio, where you can't enter non-breaking spaces either. FTFY.

So with my little tool YOU CAN. Just run the program, then Press Ctrl+Space in any text editor and the space inserted will be non-breaking one. It generally looks just like a normal space, but word wrapping won't break on that one. Add it to windows AutoStart to make it run on Windows start. In case Ctrl+Space shortcut conflicts with any other software, just use the NSBP icon in system tray to exit program.

It's very useful when preparing Unicode text for HTML, XML, XAML or others.

Of course, the text format you use must support any kind of Unicode.

### Point of interest

#### The Unicode character itself

In word processing and digital typesetting, a non-breaking space (" "), also called no-break space, non-breakable space (NBSP), hard space, or fixed space, non-breaking space is a space character that prevents an automatic line break at its position. In some formats, including HTML, it also prevents consecutive whitespace characters from collapsing into a single space.

In HTML, the common non-breaking space, which is the same width as the ordinary space character, is encoded as `&nbsp;` or `&#160;`. In Unicode, it is encoded as `U+00A0`.

#### Inserting it anywhere (in active, focused window control)

First we have to get the focused window control handle. It is not the same as the main program window handle. The main program window can contain child windows that are active. We need the focused one.
Then we have to send text to the input control. But... Windows does not provide any normal keyboard support for it. The trick is to copy our special character into the system clipboard, then paste it. Normally, by sending `WM_KEYDOWN` messages with `Ctrl` and `V` key codes. Then we need to prevent the `Space` being inserted when `Space` key is pressed with `Ctrl` key. We need to handle the shortcut globally in code.

All the Win32 magic is preformed within GlobalInput class, see source.

#### Windowless application

To create a windowless "tray application" we extend `ApplicationContext` class, make it create a `NotifyIcon` with a `ContentMenuStrip`.
Its instance can then be passed to `Application.Run()` method in main entry point.

That's it. Easy as pie.

### License

```txt
DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE
Version 2, December 2004

Copyright (C) 2004 Sam Hocevar <sam@hocevar.net>

Everyone is permitted to copy and distribute verbatim or modified
copies of this license document, and changing it is allowed as long
as the name is changed.

DO WHAT THE FUCK YOU WANT TO PUBLIC LICENSE
TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

0. You just DO WHAT THE FUCK YOU WANT TO.
```

### Disclaimer

_I'm not responsible for any damage caused by using this program or it's portions._

`GlobalInput` class uses some unmanaged code to make system voodoo, but I consider
it safe if used properly, and by properly I mean to dispose the instance before
exiting the application. You also should not block the global event handler.
Well, it's called every time a key is pressed in your whole damn system, so if you
break it, you'll probably have to restart the system ;)

The `GlobalInput` class is a part of the `Woof Toolkit` that is (probably) not yet publicly
released. If you're curious like the killed cat, it's a .NET Framework extension
that allows me to code DRY. It contains everything but the kitchen sink not specific
to any business logic, but rather to system and .NET Framework itself.
