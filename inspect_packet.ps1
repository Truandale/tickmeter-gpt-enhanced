 = \ C:\Users\Predator\Documents\tickmeter-master\tickmeter-master — gpt-fixed tcp ping\packages\Pcap.Net.x64.1.0.4.1\lib\net45\PcapDotNet.Packets.dll\
Add-Type -Path 
 = [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
[PcapDotNet.Packets.Packet].GetMembers() | Where-Object { .Name -like \*time*\ } | Select-Object Name, MemberType
