﻿using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Catdocs.Lib.OpenAPI.Internal;

internal class OpenApiDocSplitter
{
    private OpenApiDocument _document;
    private string _outputDir;
    private long _splitTime;
    private OpenApiFormat _format;
    private OpenApiSpecVersion _version;

    public OpenApiDocSplitter(
        string outputDir,
        OpenApiDocument document,
        OpenApiSpecVersion version = OpenApiSpecVersion.OpenApi3_0,
        OpenApiFormat format = OpenApiFormat.Yaml
        )
    {
        ArgumentNullException.ThrowIfNull(document, nameof(document));

        _document = document;
        _version = version;
        _format = format;

        _outputDir = outputDir;
        CreateDirIfNotExists(_outputDir);
    }


    public void Split()
    {
        if (!_document.Paths.Any())
        {
            SpecLogger.Log("No API Paths found!");
            return;
        }
        
        var paths_dir = Path.Combine(_outputDir, OpenApiConstants.Path_Dir);
        CreateDirIfNotExists(paths_dir);
        
        var paths = _document.Paths;
        _document.Paths = new OpenApiPaths();
        
        foreach (var path in paths)
        {
            var filename = Path.Combine(
                paths_dir,
                $"{GetNormalizedOpenApiPathFilename(path.Key)}.{_format.GetFormatFileExtension()}");

            try
            {
                var temp_document = new OpenApiDocument
                {
                    Paths = new OpenApiPaths()
                };
                
                temp_document.Paths.Add(path.Key, path.Value);
                temp_document.SaveDocumentToFile(_version, _format, filename);
                
                // var content = path.Value.SerializeElement(_version, _format);
                // SaveToFile(filename, content);
                
                _document.Paths.Add(path.Key, new OpenApiPathItem
                {
                    Reference = new OpenApiReference
                    {
                        Id = path.Key,
                        Type = OpenApiConstants.Path.GetOpenApiReferenceType(),
                        ExternalResource = GetRelativePath(filename)
                    }
                });
            }
            catch (Exception ex)
            {
                SpecLogger.Log($"{nameof(Split)} Exception: {ex.GetBaseException().Message}");
            }
            finally
            {
                SpecLogger.Log($"Exported API Path: {path.Key} to {filename}");
            }
        }
        
        SpecLogger.Log("Export API Paths finished.");
        
        ExportComponents();
        
        var documentFilename = $"{_outputDir}{Path.DirectorySeparatorChar}OpenApi.{_format.GetFormatFileExtension()}";
        _document.SaveDocumentToFile(_version, _format, documentFilename);
        SpecLogger.Log($"Main document created at : {documentFilename}");
    }
    
    private void ExportComponents()
    {
        ExportSchemas();
        ExportCallbacks();
        ExportParameters();
        ExportHeaders();
        ExportLinks();
        ExportResponses();
        ExportRequestBodies();
        ExportLinks();
        ExportExamples();
        //ExportSecuritySchemes();
    }

    private void ExportSchemas()
    {
        if (!_document.Components.Schemas.Any())
        {
            SpecLogger.Log("No Schema found!");
            return;
        }
        
        Export(_document.Components.Schemas);
    }

    private void ExportParameters()
    {
        if (!_document.Components.Parameters.Any())
        {
            SpecLogger.Log("No Parameters found!");
            return;
        }
        
        Export(_document.Components.Parameters);
    }

    private void ExportExamples()
    {
        if (!_document.Components.Examples.Any())
        {
            SpecLogger.Log("No Examples found!");
            return;
        }
        
        Export(_document.Components.Examples);
    }

    private void ExportSecuritySchemes()
    {
        if (!_document.Components.SecuritySchemes.Any())
        {
            SpecLogger.Log("No SecuritySchemes found!");
            return;
        }
        
        Export(_document.Components.SecuritySchemes);
    }

    private void ExportHeaders()
    {
        if (!_document.Components.Headers.Any())
        {
            SpecLogger.Log("No Headers found!");
            return;
        }
        
        Export(_document.Components.Headers);
    }

    private void ExportResponses()
    {
        if (!_document.Components.Responses.Any())
        {
            SpecLogger.Log("No Response found!");
            return;
        }
        
        Export(_document.Components.Responses);
    }

    private void ExportLinks()
    {
        if (!_document.Components.Links.Any())
        {
            SpecLogger.Log("No Links found!");
            return;
        }
        
        Export(_document.Components.Links);
    }

    private void ExportCallbacks()
    {
        if (!_document.Components.Callbacks.Any())
        {
            SpecLogger.Log("No Callbacks found!");
            return;
        }
        
        Export(_document.Components.Callbacks);
    }

    private void ExportRequestBodies()
    {
        if (!_document.Components.RequestBodies.Any())
        {
            SpecLogger.Log("No RequestBody found!");
            return;
        }
        
        Export(_document.Components.RequestBodies);
    }
    
    private void Export<T>(IDictionary<string, T> elements) where T : IOpenApiReferenceable
    {
        string elementTypeName = typeof(T).GetOpenApiElementTypeName();
        string dir = Path.Combine(_outputDir, typeof(T).GetOpenApiElementDirectoryName());
        
        CreateDirIfNotExists(dir);
        
        foreach (var el in elements)
        {
            var filename = Path.Combine(dir, $"{el.Key}.{_format.GetFormatFileExtension()}");
            
            try
            {
                var temp_document = new OpenApiDocument
                {
                    Components = new OpenApiComponents()
                };

                switch (elementTypeName)
                {
                    case OpenApiConstants.Schema:
                        temp_document.Components.Schemas.Add(el.Key, el.Value as OpenApiSchema);
                        break;
                    
                    case OpenApiConstants.Example:
                        temp_document.Components.Examples.Add(el.Key, el.Value as OpenApiExample);
                        break;
                    
                    case OpenApiConstants.Callback:
                        temp_document.Components.Callbacks.Add(el.Key, el.Value as OpenApiCallback);
                        break;
                    
                    case OpenApiConstants.Header:
                        temp_document.Components.Headers.Add(el.Key, el.Value as OpenApiHeader);
                        break;
                    
                    case OpenApiConstants.Link:
                        temp_document.Components.Links.Add(el.Key, el.Value as OpenApiLink);
                        break;
                    
                    case OpenApiConstants.Response:
                        temp_document.Components.Responses.Add(el.Key, el.Value as OpenApiResponse);
                        break;
                    
                    case OpenApiConstants.RequestBody:
                        temp_document.Components.RequestBodies.Add(el.Key, el.Value as OpenApiRequestBody);
                        break;
                    
                    case OpenApiConstants.Parameter:
                        temp_document.Components.Parameters.Add(el.Key, el.Value as OpenApiParameter);
                        break;
                    
                    case OpenApiConstants.SecurityScheme:
                        temp_document.Components.SecuritySchemes.Add(el.Key, el.Value as OpenApiSecurityScheme);
                        break;
                }

                temp_document.SaveDocumentToFile(_version, _format, filename);
                
                //var content = el.Value.SerializeElement(_version, _format);
                //SaveToFile(filename, content);

                //_document.Components.AddExternalReferenceFor(elementTypeName, el.Key, filename);
            }
            catch (Exception ex)
            {
                SpecLogger.LogException(elementTypeName, ex);
            }
            finally
            {
                SpecLogger.Log($"Exported {elementTypeName}: {el.Key} to {filename}");
            }
        }
        
        _document.Components.DeleteAllElementsOfType(elementTypeName);
        
        SpecLogger.Log($"Export {elementTypeName} finished.");
    }

    private static string GetNormalizedOpenApiPathFilename(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            //TODO: log error
            throw new ArgumentNullException(nameof(path));
        }

        return path.TrimStart('/').Replace('/', '_');
    }
    
    private static void CreateDirIfNotExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    internal string GetRelativePath(string filename)
    {
        var fullFilePath = Path.GetFullPath(filename);
        var fullBasePath = Path.GetFullPath(_outputDir);

        var filePathUri = new Uri(fullFilePath);
        var basePathUri = new Uri(fullBasePath + Path.DirectorySeparatorChar);

        var relativeUri = basePathUri.MakeRelativeUri(filePathUri);

        // var result = Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        var result = Uri.UnescapeDataString(relativeUri.ToString());
        return result;
    }
}