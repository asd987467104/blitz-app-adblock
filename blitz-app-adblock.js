const fs = require('fs');
const asar = require('asar');
const path = require('path');

const io = require('./io');
const js = require('./js');

var appPath = '';
var noUpdate = false;
var autoGuest = false;

async function start() {
    try {
        argumentsHandler();
        
        // mac os app path
        if (process.platform === 'darwin') {
            appPath = '/Applications/Blitz.app/Contents/Resources';
        }
        // windows app path
        else if (process.platform === 'win32') {
            appPath = path.join(process.env.APPDATA, '../Local/Programs/Blitz/resources');
        }

        if(!appPath || !fs.existsSync(`${appPath}/app.asar`)) {
            console.log("app.asar not found!");
        }
        else {
            console.log('Extracting app.asar...');
            await asar.extractAll(`${appPath}/app.asar`, `${appPath}/app/`);
        
            console.log('Downloading ad & tracking filters...');
            await io.downloadFile('https://easylist.to/easylist/easylist.txt', `${appPath}/app/src/easylist.txt`);
            await io.downloadFile('https://easylist.to/easylist/easyprivacy.txt', `${appPath}/app/src/easyprivacy.txt`);
            await io.downloadFile('https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/filters.txt', `${appPath}/app/src/ublock-ads.txt`);
            await io.downloadFile('https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/privacy.txt', `${appPath}/app/src/ublock-privacy.txt`);
            await io.downloadFile('https://pgl.yoyo.org/adservers/serverlist.php?hostformat=adblock&showintro=1&mimetype=plaintext', `${appPath}/app/src/peter-lowe-list.txt`);
        
            console.log('Patching...');
    
            // copy adblocker lib to src
            if(fs.existsSync(`adblocker.umd.min.js`)) 
                io.copyFile('adblocker.umd.min.js', `${appPath}/app/src/adblocker.umd.min.js`)
            else io.copyFile('./build/adblocker.umd.min.js', `${appPath}/app/src/adblocker.umd.min.js`)
    
            // start writing our payload to createWindow.js
            io.modifyFileAtLine(js.filterEngine, `${appPath}/app/src/createWindow.js`, 119);
            io.modifyFileAtLine('session: true,', `${appPath}/app/src/createWindow.js`, 106);
    
            // optional features
            if (noUpdate)  io.ModifyFileAtLine('if (false) {', `${appPath}/app/src/index.js`, 267);
            if (autoGuest) io.ModifyFileAtLine(js.autoGuest, `${appPath}/app/src/preload.js`, 18);
    
            // repack
            console.log('Repacking app.asar...');
            await asar.createPackage(`${appPath}/app/`, `${appPath}/app.asar`);

            // cleanup
            console.log('Cleaning up directory...');
            io.deleteFolder(`${appPath}/app/`);
            
            console.log('\r\nPatching complete! GLHF :)');
        }
    }
    catch (error) {
        console.log(error);
    }

    console.log();
    require('readline')
        .createInterface(process.stdin, process.stdout)
        .question('Press ENTER to quit...', function(){
            process.exit();
    });
}

function argumentsHandler() {
    var args = process.argv.slice(2);
    for (var i = 0; i < args.length; i++) {
        switch (args[i]) {
        case '-noupdate':
            noUpdate = true;
            break;
        case '-autoguest':
            autoGuest = true;
            break;
        default:
            console.log(`Unknown argument: '` + args[i] + `'`);
        }
    }
}

start();