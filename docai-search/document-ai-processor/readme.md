## Required Setup

* [MUST follow this introduction to setup Cloud Search](https://developers.google.com/cloud-search/docs/guides/project-setup)

* (Optional)[Configure Domain-Wide Delegation](https://admin.google.com/u/2/ac/owl/domainwidedelegation)

* Create an `appsettings.json` with below schema under source root

```json


{
    "fileSystem":{
        "folderPath":"<Filder Path of your files such as : /user/Documents/some-folder>",
        "searchSubFolders":"<true|false : placeholder only>"
    },
    "integration":{
        "DocumentAI":{
            "serviceAccountEmail":"<Service account email with access to Document AI API and GCS bucket>",
            "keyFile":"./secrets/docai-key.p12",
            "password":"notasecret",
            "gcs":"<GCS Bucket to store files : gs://some-bucket>",
            "gcs_project":"<GCP project Id>",
            "operation_url":"https://documentai.googleapis.com/v1beta3/projects/<Project Number>/locations/us/operations/",
            "small_ocr_url":"https://us-documentai.googleapis.com/v1beta3/projects/<Project Number>/locations/us/processors/<Processor ID>:process",
            "large_formParser_url": "https://us-documentai.googleapis.com/v1beta3/projects/<Project Number>/locations/us/processors/<Processor ID>:batchProcess"
        },
        "CloudSearch":
        {
            "serviceAccountEmail":"<Service account email with access to Cloud Search API and GCS bucket>",
            "keyFile":"./secrets/key.p12",
            "password":"notasecret",
            "url":"https://cloudsearch.googleapis.com/v1/indexing/datasources/<Data Source Id>/items",
            "datasource_id":"<Data Source Id>"
        }
    }
}
```

* Create Service Account and Download certificates to `secrets` folder