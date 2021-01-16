## Required Setup

* [MUST follow this introduction to setup Cloud Search](https://developers.google.com/cloud-search/docs/guides/project-setup)

* Create an `appsettings.json` with below schema under source root

```json
{
    "fileSystem":{
        "folderPath":"<Filder Path of your files>",
        "searchSubFolders":"<true | false (the sample codes may not already handle sub-folders yet)>"
    },
    "integration":{
        "DocumentAI":{

        },
        "CloudSearch":
        {
            "serviceAccountEmail":"<Service Account Email - follow above doc>",
            "keyFile":"./secrets/key.p12",
            "url":"https://cloudsearch.googleapis.com/v1/indexing/datasources/<datasource_id>/items",
            "datasource_id":"<Datasource Id>"
        }
    }
}
```

* Create Service Account and Download certificates to `secrets` folder