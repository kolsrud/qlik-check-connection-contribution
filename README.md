# qlik-check-connection-contribution

This tool provides an indication of how much time of the repository side of the app open flow that is spent on evaluating security rules for data connections. The tool can be run on any node in a Qlik Sense deployment, but must be run as an account that has access to the Qlik Sense certificates used for Qlik Sense service communications. The tool should therefore typically be run as the service account running the Qlik Sense services. Two arguments need to be provided on the command line:

1. The user to impersonate (preferably a user experiencing slow app open times) in the format DOMAIN\id.
2. The ID of an app that is accessible to the user defined in 1.

Usage:   .\QlikCheckConnectionContributions {USERDIR}\\{userid} {appId}\
Example: .\QlikCheckConnectionContributions INTERNAL\sa_api 4a03b166-af2c-4784-b17b-1b22dcf5ed4c
