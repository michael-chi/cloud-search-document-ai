var express = require('express')
var bodyParser = require('body-parser')
const expressApp = express().use(bodyParser.json());

expressApp.get('/openfile', (req, resp) => {
    let file = req.query.file;
    let fn = file.split('/')[file.split('/').length - 1];
    console.log(fn);

    resp.download(file, fn);
});
expressApp.listen(8081);    //  Cloud Run now only supports port 8080
console.log(`express server listening on port 8081...`);