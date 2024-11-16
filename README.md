# GEND.AR
[GEND.AR](https://gend.ar): Privacy-Preserving Gender-Inclusive Matching in Extended Reality for Spontaneous In-Person Encounters Using Secure Two Party Computation

## Slogan

Envision a Flourishing Future of Privacy-Preserving Gender-Inclusive Social Encounters

## Abstract

Spontaneous social encounters in contemporary society present a coordination challenge around gender and identity. While some people rely on visual cues and stereotypes (colloquially known as '[gaydar](https://en.wikipedia.org/wiki/Gaydar)') to infer others' [gender identity](https://en.wikipedia.org/wiki/Gender_identity), [gender expression](https://en.wikipedia.org/wiki/Gender_expression), [sexual orientation](https://en.wikipedia.org/wiki/Sexual_orientation), or [preferred gender pronouns](https://en.wikipedia.org/wiki/Preferred_gender_pronoun), such assumptions are both unreliable and potentially harmful. This creates a social dilemma: direct questions about personal identity may feel invasive, yet incorrect assumptions can damage relationships and cause distress. The challenge lies in navigating these interactions respectfully while acknowledging both individual privacy and the need for authentic recognition.

We propose [GEND.AR](https://gend.ar), an extended reality application that addresses this social coordination challenge through privacy-preserving technology. The system enables users to discover compatible individuals within their social proximity (10-20 meters) without compromising personal information. Using [secure two-party computation protocols](https://en.wikipedia.org/wiki/Secure_two-party_computation) over Apple's [MultipeerConnectivity](https://developer.apple.com/documentation/multipeerconnectivity) framework (technology behind AirDrop), GEND.AR performs encrypted compatibility calculations when users are nearby. These calculations consider sexual orientation, gender identity, availability, and visibility preferences, without revealing individual attributes to anyone—including non-matching users and third parties. All personal information remains exclusively on users' devices, eliminating the need for centralized servers.

Upon detecting a mutual match, the extended reality interface displays a subtle, calming animated visual indicator connecting the matched users. This [minimalist](https://en.wikipedia.org/wiki/Calm_technology) approach avoids intrusive notifications while maintaining discretion. Non-matching users receive no notifications, thereby preserving privacy and preventing social awkwardness. To ensure authenticity and prevent misuse, the system employs [zero-knowledge proof](https://en.wikipedia.org/wiki/Zero-knowledge_proof) technology through platforms like [OpenPassport](https://www.openpassport.app/) and [WorldCoin](https://world.org/), requiring proof of personhood through bio-verification or passport-proof identities. While the system supports gender fluidity, it maintains accountability by tracking identity changes—users' identity modification patterns are visible to potential matches, and frequent changes are flagged to prevent potential misuse of the platform.

As an [experiential futures](https://dl.designresearchsociety.org/drs-conference-papers/drs2024/researchpapers/359/) design, GEND.AR explores potential technological solutions for facilitating authentic social connections in gender-inclusive spaces. While current technological and social constraints may limit immediate widespread adoption, this speculative project aims to spark discussion about how privacy-preserving technology could support authentic social connections while maintaining individual dignity and agency in future gender-inclusive spaces. 



## Venue

[Edge City Lanna Hachathon](https://dorahacks.io/hackathon/edgecitylanna/) with [GEND.AR BUILD](https://dorahacks.io/buidl/18756) 


## Techological Stack 

* Game Engine: [Unity 6 LTS](https://unity.com/releases/unity-6-releases)
* Hardware:
  * [HoloKit](https://holokit.io) MR headset for spatial computing
  * iPhone 15 Pro for computational processing
* Network Layer: Apple's [MultipeerConnectivity](https://developer.apple.com/documentation/multipeerconnectivity)
* Identity Verification (Zero-Knowledge Proof): 
  * [WorldCoin](https://world.org/) (Bio-verification) 
  * [OpenPassport](https://www.openpassport.app/) (Passport-proof identities)

* Core Protocol: 
  * We employed a secure two-party computation (2PC) [Cursive Team's 2P-PSI](https://github.com/cursive-team/2P-PSI) that:
    * Enables private set interaction between users
Maintains privacy of user preferences
    * Operates in real-time within spatial computing constraints

## Implementations
GEND.AR has been prototyped as a proof-of-concept mixed reality application combining privacy-preserving computation with spatial computing. Built on Unity 6 LTS and targeting the HoloKit mixed reality headset paired with iPhone 15 Pro, our implementation leverages HoloKit's optical system for immersive experiences and the iPhone's Neural Engine for high-performance processing. The core functionality centers on a secure two-party computation protocol operating over Apple's MultipeerConnectivity framework, enabling private set intersection between users while maintaining preference confidentiality. Currently in the testing phase, our team is focused on fine-tuning the user experience, optimizing computational performance, reducing power consumption, minimizing interaction latency, and validating protocol security to advance privacy-conscious mixed reality social interactions.

## Related Works

This work draws inspiration from Barry Whitehat's influential presentation ["2PC is for Lovers"](https://www.youtube.com/watch?v=PzcDqegGoKI) delivered at PROGCRYPTO 2023.


## Team 

[Botao 'Amber' Hu](https://botao.hu) is a researcher, designer, educator, and creative technologist. He directs Reality Design Lab, an independent interdisciplinary research and design lab exploring the intersection of soma design (bodily play), speculative design (future studies and artificial life), spatial computing (mixed reality), and social computing (public-interest cryptographic tech). His primary focus is designing social experiential futures using mixed reality as the main medium. He also serves as a visiting lecturer at the China Academy of Art. His works have been featured at top conferences such as SIGGRAPH, CHI, UbiComp, CSCW, WWW, IEEE VIS, ALIFE, ISMAR, Ars Electronica, SXSW, and TEDx and have received accolades including the SIGGRAPH Best in Show, CHI Best Interactivity, Webby, Red Dot, iF Design, A' Design, Core77 Design Award, and grants from the Ethereum Foundation. He holds a bachelor's degree in computer science from Tsinghua University and a master's degree in computer science with AI concentration from Stanford University.


## Acknowledgements

Vivek and Andrew from Cursive Team for their support.

## License

MIT License


## Gender Compatibility Matrix

Here's a gender compatibility and attraction matrix in markdown format:

| Attracted to → <br>Identity ↓ | Cis Women | Cis Men | Trans Women | Trans Men | Non-Binary | Agender |
|------------------------------|------------|----------|-------------|-----------|------------|----------|
| **Heterosexual Woman**       | ❌         | ✅       | ✅          | ✅        | ❓         | ❓       |
| **Heterosexual Man**         | ✅         | ❌       | ✅          | ❌        | ❓         | ❓       |
| **Gay Man**                  | ❌         | ✅       | ❌          | ✅        | ❓         | ❓       |
| **Lesbian Woman**            | ✅         | ❌       | ✅          | ❌        | ❓         | ❓       |
| **Bisexual**                 | ✅         | ✅       | ✅          | ✅        | ✅         | ✅       |
| **Pansexual**               | ✅         | ✅       | ✅          | ✅        | ✅         | ✅       |
| **Asexual**                 | ❌         | ❌       | ❌          | ❌        | ❌         | ❌       |

Legend:
- ✅ Typically attracted
- ❌ Typically not attracted
- ❓ Varies by individual

Note: This is a simplified matrix and attraction patterns can be more complex and individual. Some people may identify differently or experience attraction outside these typical patterns.
