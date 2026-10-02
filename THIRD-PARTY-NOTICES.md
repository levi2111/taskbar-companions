# Third-party notices

Taskbar Companions' own code is under the [MIT License](LICENSE). It uses no third-party packages.

## Included in the downloads

The installer and the portable zip include the .NET runtime, with Windows Presentation Foundation and Windows Forms, so you don't have to install .NET yourself.

| Component | License | Source |
|---|---|---|
| .NET Runtime | MIT | https://github.com/dotnet/runtime |
| Windows Presentation Foundation (WPF) | MIT | https://github.com/dotnet/wpf |
| Windows Forms | MIT | https://github.com/dotnet/winforms |

The downloads' `licenses` folder holds these components' license texts and the .NET Runtime's notices for the third-party code inside it. WPF and Windows Forms list theirs in their repositories: [WPF](https://github.com/dotnet/wpf/blob/main/THIRD-PARTY-NOTICES.TXT), [Windows Forms](https://github.com/dotnet/winforms/blob/main/THIRD-PARTY-NOTICES.TXT).

```text
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Not included, and not covered by this project's license

- **Clawd** is Anthropic's character. `Clawd.cs` draws a fan redrawing of it in code. The MIT License covers that code, but not Anthropic's rights in the character or in the names Claude, Claude Code and Clawd.
- **The Codex pet artwork** belongs to OpenAI. It isn't part of this repository or the downloads: the app reads it from the Codex extension you have installed.
- **Claude Code, Codex and Node.js** work with the app when you have them, but aren't included. The status line bridge uses only Node.js's built-in modules.
