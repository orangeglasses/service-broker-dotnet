# service-broker-dotnet
Implementation of an in-memory OSBAPI service broker written in dotnet.

The service broker expects a configuration setting named `Authentication:Password` to exist in the environment. This setting will be used as the basic authentication password by which the platform authenticates against the service broker. The corresponding username is `rwwilden-broker`.