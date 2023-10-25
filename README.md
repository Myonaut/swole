Developed in Unity 2021.3 LTS for the Universal Render Pipeline (URP)

All scripts in the Source-Main folder should work outside of a Unity environment. Scripts in the Source-Unity folder should void themselves when there's no Unity environment present.

When using Swole outside of a Unity environment, you must define the conditional compilation symbol 'SWOLE_ENV' in order for some scripts to function correctly.