# DNN CAS Authentication provider
### Latest release [![Latest release](https://intelequia.blob.core.windows.net/images/DNNAzureAD_3.0.1.svg)](https://github.com/intelequia/dnn.casprovider/releases/latest)

## Contents
- [Overview](#overview)
- [Requirements](#requirements)
- [Installation and configuration guide](#installation-and-configuration-guide)
  - [DNN Provider installation and configuration](#provider-configuration)

<a name="overview"></a>
## Overview
The DNN CAS Authentication Provider is an Authentication provider for DNN Platform that uses CAS OAuth2 flows to authenticate users.

Enterprise Single Sign-On - CAS provides a friendly open source community that actively supports and contributes to the project. While the project is rooted in higher-ed open source, it has grown to an international audience spanning Fortune 500 companies and small special-purpose installations.

For more information about CAS, see https://www.apereo.org/projects/cas

<a name="requirements"></a>
## Requirements
* **DNN Platform 9.0.0 or later**

<a name="installation-and-configuration-guide"></a>
## Installation and configuration guide

Following these steps, you will give access to all your CAS users to register and sign-in into your DNN application. You can go later and harden your security settings like only allow to access the DNN Web Application to certain users on the CAS management. Also note that depending on whether you have "Public" or "Private" user account registration on your DNN portal, you will need to approve the registered user before allowing him to access the site.

<a name="provider-configuration"></a>
### DNN provider installation and configuration
It's important to remember that you need a DNN deployment with **version 9.0.0 or later** to continue. 

1. Download the DNN CAS Auth provider from the Releases folder (i.e. CasProvider_01.00.00_Install.zip) https://github.com/intelequia/dnn.casprovider/releases
2. Login into your DNN Platform website as a host user and install the provider from the "Host > Extensions" page
3. Use the **Install Extension Wizard** to upload and install the file you downloaded on step 1. Once installed, you can setup the provider from the new settings page, under the section **CAS Settings** on the Persona Bar:

The settings page is very straightforward. It only requires three parameters from your CAS service:
* **CAS Server URL**: Specify the server URL (i.e. https://cas.example.org:8443/)
* **App ID**: This is the **Application ID** of the service application 
* **Secret**: This is the **Key** of the service application
* **Enabled**: Use this switch to enable/disable the provider
* **Auto-Redirect**: This option allows you to automatically redirect your login page to the CAS login page

## Building the solution
### Requirements
* Visual Studio 2019 (download from https://www.visualstudio.com/downloads/)
* npm package manager (download from https://www.npmjs.com/get-npm)

### Configure local npm to use the DNN public repository
From the command line, the following command must be executed:
```
   npm config set registry https://www.myget.org/F/dnn-software-public/npm/
```
### Install package dependencies
From the comman line, enter the <RepoRoot>\CasProvider\Cas.Web and run the following commands:
```
  npm install -g webpack
  npm install
```

### Build the module
Now you can build the solution by opening the RedisCachingProvider.sln file on Visual Studio 2019. Building the solution in "Release", will generate the React bundle and package it all together with the installation zip file, created under the "\releases" folder.

On the Visual Studio output window you should see something like this:
```
1>------ Rebuild All started: Project: DotNetNuke.Authentication.Cas, Configuration: Release Any CPU ------
1>C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\Microsoft.Common.CurrentVersion.targets(2106,5): warning MSB3277: Found conflicts between different versions of "Newtonsoft.Json" that could not be resolved.  These reference conflicts are listed in the build log when log verbosity is set to detailed.
1>  DotNetNuke.Authentication.Cas -> C:\Dev\dnn.casprovider\DotNetNuke.Authentication.Cas\bin\DotNetNuke.Authentication.Cas.dll
1>  Browserslist: caniuse-lite is outdated. Please run next command `npm update`
1>  Hash: 75943401875419dda207
1>  Version: webpack 4.33.0
1>  Time: 3555ms
1>  Built at: 03/05/2020 20:24:59
1>             Asset      Size  Chunks             Chunk Names
1>      bundle-en.js  45.4 KiB       0  [emitted]  main
1>  bundle-en.js.map  91.5 KiB       0  [emitted]  main
1>  Entrypoint main = bundle-en.js bundle-en.js.map
1>   [0] external "window.dnn.nodeModules.React" 42 bytes {0} [built]
1>   [1] external "window.dnn.nodeModules.CommonComponents" 42 bytes {0} [built]
1>   [2] ./src/utils/index.jsx 207 bytes {0} [built]
1>   [3] ./src/constants/actionTypes/index.js + 1 modules 288 bytes {0} [built]
1>       | ./src/constants/actionTypes/index.js 55 bytes [built]
1>       | ./src/constants/actionTypes/settings.js 233 bytes [built]
1>   [4] external "window.dnn.nodeModules.ReactRedux" 42 bytes {0} [built]
1>   [5] external "window.dnn.nodeModules.Redux" 42 bytes {0} [built]
1>   [9] external "window.dnn.nodeModules.ReactDOM" 42 bytes {0} [built]
1>  [10] external "window.dnn.nodeModules.ReduxThunk" 42 bytes {0} [built]
1>  [11] external "window.dnn.nodeModules.ReduxImmutableStateInvariant" 42 bytes {0} [built]
1>  [12] external "window.dnn.nodeModules.ReduxDevTools" 42 bytes {0} [built]
1>  [13] external "window.dnn.nodeModules.ReduxDevToolsLogMonitor" 42 bytes {0} [built]
1>  [14] external "window.dnn.nodeModules.ReduxDevToolsDockMonitor" 42 bytes {0} [built]
1>  [15] ./src/containers/Root.js 170 bytes {0} [built]
1>  [25] ./src/containers/Root.prod.js + 6 modules 18.4 KiB {0} [built]
1>       | ./src/containers/Root.prod.js 2.62 KiB [built]
1>       | ./src/actions/settings.js 1.89 KiB [built]
1>       | ./src/services/applicationService.js 1.57 KiB [built]
1>       | ./src/resources/index.jsx 186 bytes [built]
1>       |     + 3 hidden modules
1>  [26] ./src/main.jsx + 5 modules 4.21 KiB {0} [built]
1>       | ./src/main.jsx 544 bytes [built]
1>       | ./src/store/configureStore.js 552 bytes [built]
1>       | ./src/globals/application.js 590 bytes [built]
1>       | ./src/reducers/rootReducer.js 171 bytes [built]
1>       | ./src/containers/DevTools.js 382 bytes [built]
1>       | ./src/reducers/settingsReducer.js 1.97 KiB [built]
1>      + 12 hidden modules
========== Rebuild All: 1 succeeded, 0 failed, 0 skipped ==========

```


## References
* CAS project(https://www.apereo.org/projects/cas) 
