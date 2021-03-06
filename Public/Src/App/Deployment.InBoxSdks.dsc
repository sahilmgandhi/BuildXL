// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const sdkRoot = Context.getMount("SdkRoot").path;

@@public
export const inBoxSdks = createSdkDeploymentDefinition(false);
export const inBoxServerSdks = createSdkDeploymentDefinition(true);

function createSdkDeploymentDefinition(serverDeployment: boolean) : Deployment.Definition {

    return {
        contents: [
            {
                subfolder: "Sdk",
                contents: [
                    {
                        subfolder: "Sdk.Prelude",
                        contents: glob(d`${sdkRoot}/Prelude`, "*.dsc")
                    },
                    {
                        subfolder: "Sdk.Transformers",
                        contents: glob(d`${sdkRoot}/Transformers`, "*.dsc")
                    },
                    {
                        subfolder: "Sdk.QTest",
                        contents: [
                            ...addIfLazy(!serverDeployment && qualifier.targetRuntime === "win-x64", () => [
                                importFrom("BuildXL.Tools.QTest").deployment
                            ]),
                        ],
                    },
                    {
                        subfolder: "Sdk.Drop",
                        contents: [
                            ...addIfLazy(!serverDeployment && !BuildXLSdk.isTargetRuntimeOsx, () => [
                                importFrom("BuildXL.Tools.DropDaemon").withQualifier({
                                    targetFramework: "net472",
                                    targetRuntime: "win-x64"
                                }).deployment
                            ])
                        ],
                    },
                    {
                        subfolder: "Sdk.Symbols",
                        contents: [
                            ...addIfLazy(!serverDeployment && !BuildXLSdk.isTargetRuntimeOsx, () => [
                                importFrom("BuildXL.Tools.SymbolDaemon").withQualifier({
                                    targetFramework: "net472",
                                    targetRuntime: "win-x64"
                                }).deployment
                            ])
                        ],
                    },
                ]
            }
        ]
    };
}
