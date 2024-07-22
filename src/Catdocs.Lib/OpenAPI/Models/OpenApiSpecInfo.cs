﻿namespace Catdocs.Lib.OpenAPI;

public record OpenApiSpecInfo(
    string InputFile,
    bool HasErrors,
    string Version,
    string Format,
    bool IsJson,
    bool IsYaml,
    List<string> Errors,
    long ParseTime
)
{
    public bool Success => !HasErrors;
}