## NOTE: We have migrated reported issues to FogBugz. You can only log further issues via the Unity bug tracker. To see how, read [this](https://unity3d.com/unity/qa/bug-reporting).

# Unity Scriptable Render Pipeline
The Scriptable Render Pipeline (SRP) is a Unity feature designed to give artists and developers the tools they need to create modern, high-fidelity graphics in Unity. Unity provides two pre-built Scriptable Render Pipelines:

* The Universal Render Pipeline (URP) for use on all platforms.
* The High Definition Render Pipeline (HDRP) for use on compute shader compatible platforms.

Unity is committed to an open and transparent development process for SRP and the pre-built Render Pipelines. This means that so you can browse this repository to see what features are currently in development.

For more information about the packages in this repository, see the following:

* [Scriptable Render Pipeline Core](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/index.html)
* [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html)
* [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html)
* [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html)
* [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html)

### Package CI Summary

Package Name | Latest CI Status
------------ | ---------
com.unity.render-pipelines.core | [![](https://badge-proxy.cds.internal.unity3d.com/658ed6e2-cb73-4c17-909e-1c558e402f15)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/d97b521a-469f-4c39-9176-efba794011d2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/74b65e22-f1c3-4b3a-a6e9-6c1528314bc4)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/dependants-info)[![](https://badge-proxy.cds.internal.unity3d.com/ce5c4776-4467-468a-9251-ce9f232b3fdd)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.core/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/90be70c3-cd3c-4275-940c-8ca0262fb711) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/73c999ed-fd64-4df1-a6b8-77df8cbfe50f)
com.unity.render-pipelines.universal | [![](https://badge-proxy.cds.internal.unity3d.com/83da995f-a4e5-46d7-8965-4dd38cc6d0a2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/4758e59a-2dcb-40ee-8e41-6b779340a25b)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/2eaeea22-a937-4476-ac4b-6071378be1ba)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/b15d41d5-df2b-4411-9413-7f8c8ea369be)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.universal/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5a632a87-cc88-4414-be12-394dfeb934df) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/28dfd57b-54d1-45ca-80d3-94d96dbbcfd0)
com.unity.render-pipelines.high-definition | [![](https://badge-proxy.cds.internal.unity3d.com/a8b1403f-68dd-4d9e-8198-39931007b1d2)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/1038f4d6-9ace-4e6e-aa5f-1793f222716d)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/d3ed9e4b-d9c4-4401-b952-ed5808aafe44)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/7e6d655c-ce18-4546-8f2e-6ee85583f244)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/b7d3bcae-9ad8-4375-a683-1b907828137f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/1ef3d7d0-cea1-4955-9276-e34c0952afbb)
com.unity.render-pipelines.high-definition-config | [![](https://badge-proxy.cds.internal.unity3d.com/25b27fae-b4c9-4ef9-84c2-8ca7f38c2262)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation)[![](https://badge-proxy.cds.internal.unity3d.com/2abd0987-57c9-41a4-bc89-04037fe17057)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/ab12a6a1-17e5-478f-9916-7cfe77f2dbbb)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/b5ba525c-426a-4fa8-a783-e6b0001d430c)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.high-definition-config/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59fd14b1-3fc2-49e4-bf24-950f1482323f) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/d0fb96fc-6ff8-45a8-a317-ec19f30894cc)
com.unity.shadergraph | [![](https://badge-proxy.cds.internal.unity3d.com/5f2ebc29-a76f-40e3-8f2c-9b3f19e382ce)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/f20fc57c-45d3-4990-8f1f-d311c79c0824)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/7e1ee3c6-0477-4076-a2af-3376ead10421)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/dc92bffa-83bc-49af-b432-eaf1376b8935)](https://badges.cds.internal.unity3d.com/packages/com.unity.shadergraph/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/e2171d56-50c8-4803-964c-a63dcc728355) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/30fe71f1-5838-4bf9-84eb-26a42320e4a2)
com.unity.visualeffectgraph | [![](https://badge-proxy.cds.internal.unity3d.com/bbc6e3c6-5113-451d-bab1-71c2f14ae9ef)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/77e84168-aec9-44d6-94ce-c9404d3aebcc)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/c10f50c2-2a79-4d0a-a763-54dcb40d027f)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/82a3dcaa-f077-43ed-9573-6b19327024ec)](https://badges.cds.internal.unity3d.com/packages/com.unity.visualeffectgraph/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/59b6ec9b-c477-4767-82ba-d2390e70cede) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/ae2fb4f5-43dc-4ad2-8c94-7190dbcdc132)
com.unity.render-pipelines.lightweight | [![](https://badge-proxy.cds.internal.unity3d.com/9e82bdfb-323e-4053-857d-53ae40105738)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/build-info?branch=7.x.x%2Frelease&testWorkflow=package-isolation) [![](https://badge-proxy.cds.internal.unity3d.com/c9df9d2b-7132-405a-8437-f89a74192067)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependencies-info?branch=7.x.x%2Frelease&testWorkflow=updated-dependencies) [![](https://badge-proxy.cds.internal.unity3d.com/7e4aae95-2a9a-471c-a5f8-e8faf3675454)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/dependants-info) [![](https://badge-proxy.cds.internal.unity3d.com/af483d19-8862-4ccf-ae50-48f1605cce3e)](https://badges.cds.internal.unity3d.com/packages/com.unity.render-pipelines.lightweight/warnings-info?branch=7.x.x%2Frelease) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/679931b4-d19f-4788-90af-be45f40f3a11) ![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/a11f872a-60e4-4a16-a3f7-4ac888bcd879)


## Using the latest version

This repository uses the **master** branch for main development. Development on this branch is based on the latest internal version of Unity so it may not work on the latest publicly available version of Unity. The following list contains Unity version/major SRP version pairs which you can use as a guideline as to which major SRP version you can use in your Unity Project:

- **Unity 2019.1 is compatible with SRP version 5.x**
- **Unity 2019.2 is compatible with SRP version 6.x**
- **Unity 2019.3 is compatible with SRP version 7.x**
- **Unity 2020.1 is compatible with SRP version 8.x**

The above list is a guideline for major versions of SRP, but there are often multiple minor versions that you can use for a certain version of Unity. To determine which minor versions of SRP you can use:

1. In your Unity Project, open the Package Manager window (menu: **Window > Package Manager**).
2. In the list of packages, find **Core RP Library**. To find this package in older versions of Unity, you may need to expose preview packages. To do this, click the **Advanced** button at the top of the window then, in the context menu, click **Show preview packages**.
3. Click the drop-down arrow to the left of the package entry then click **See all versions**. This shows a list that contains every package version compatible with your version of Unity.

After you decide which version of SRP to use:

1. Go to the [Scriptable Render Pipeline repository](https://github.com/Unity-Technologies/ScriptableRenderPipeline).
2. Click the **Branch** drop-down then click the **Tags** tab.
3. Find the tag that corresponds to the version of SRP you want to use. When you clone the repository, you use this tag to check out the correct branch.

To clone the repository, you can use a visual client, like [GitHub Desktop](#GitHubDesktop), or use [console commands](#ConsoleCommands). When you clone the repository, make sure to clone it outside of your Unity Project's Asset folder. 

After you clone the repository, you can install the package into your Unity Project. To do this, see [Installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html).

<a name="GitHubDesktop"></a>

### Cloning the repository using the GitHub Desktop App:

1. Open the GitHub Desktop App and click **File > Clone repository**.
2. Click the **URL** tab and enter the following URL: https://github.com/Unity-Technologies/ScriptableRenderPipeline.
3. Click the **Choose…** button and navigate to your Unity Project’s base folder.
4. Click the **Clone** button.

After you clone the repository, open your console application of choice in the ScriptableRenderPipeline folder and run the following console command:

`\> git checkout v7.1.8 (or the latest tag)`

<a name="ConsoleCommands"></a>

### Cloning the repository using console commands:

Open your console application of choice and run the following console commands:

```
\> cd <Path to your Unity project>

\> git clone https://github.com/Unity-Technologies/ScriptableRenderPipeline

\> cd ScriptableRenderPipeline

\>  git checkout v7.1.8 (or the latest tag)
```

## Sample Scenes in ScriptableRenderPipelineData

Unity provides sample Scenes to use with SRP. You can find these Scenes in the [ScriptableRenderPipelineData GitHub repository](https://github.com/Unity-Technologies/ScriptableRenderPipelineData). To add the Scenes to your Project, clone the repository into your Project's Assets folder.