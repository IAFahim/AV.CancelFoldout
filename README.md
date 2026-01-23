# AV.CancelFoldout

![Header](documentation_header.svg)

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-000000.svg?style=flat-square&logo=unity)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE.md)

Inspector extension to add a "Cancel" (X) button to foldout headers for quick clearing or resetting.

## ✨ Features

- **UI Enhancement**: Adds a small close button to foldout headers.
- **Cleaner Inspector**: Allows collapsing or clearing complex nested structures easily.
- **Non-Invasive**: Works as a PropertyDrawer.

## 📦 Installation

Install via Unity Package Manager (git URL).

## 🚀 Usage

Use the `[CancelFoldout]` attribute on your fields.

```csharp
[CancelFoldout]
public MyComplexStruct data;
```

## ⚠️ Status

- 🧪 **Tests**: Missing.
- 📘 **Samples**: Included in `Samples~`.
