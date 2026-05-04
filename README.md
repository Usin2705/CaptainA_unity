# SaySuomi - Mobile App for Finnish Pronunciation Practice

[![CI](https://github.com/Usin2705/CaptainA_unity/actions/workflows/ci.yml/badge.svg)](https://github.com/Usin2705/CaptainA_unity/actions/workflows/ci.yml)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.20021230.svg)](https://doi.org/10.5281/zenodo.20021230)

SaySuomi, previously named CaptainA, is a mobile application designed to help users practice Finnish pronunciation. This repository contains the Unity mobile app code for Android and iOS.

The app is available on [Google Play](https://play.google.com/store/apps/details?id=com.Kielibuusti.CaptainA) and the [Apple App Store](https://apps.apple.com/ph/app/captaina/id6444659467). The [demo paper](https://aclanthology.org/2023.nodalida-1.26) gives a short introduction, and the original app development is described in this [Master's thesis](http://urn.fi/URN:NBN:fi:aalto-202305213302).

## Student ASA Release (2026)

This repository includes the initial implementation of the Automatic Speaking Assessment (ASA) module developed by the 2026 student team.

The ASA module supports speaking tasks, audio recording, server-based scoring, and score review in the mobile app. The ASA server is maintained separately in the [aalto-speech/dta-server](https://github.com/aalto-speech/dta-server) repository.

A frozen version of the student contribution is archived as release [v1.0.0](https://github.com/Usin2705/CaptainA_unity/releases/tag/v1.0.0) and on [Zenodo](https://zenodo.org/records/20021230). Further development continued after this release.

For more information about the ASA implementation and release context, see [SaySuomi-ASA doc.pdf](docs/asa_release/SaySuomi-ASA%20doc.pdf).

## Citation

If you use or refer to the ASA student release, please cite it as follows.

**APA 7th**

Immonen, L., Kähkönen, A., Porola, I., Ratilainen, M., Savolainen, A., Takala, K., Tonteri, M., Immonen, R., Phan, N., & von Zansen, A. (2026). *SaySuomi: Automatic Speaking Assessment Student Release v1.0.0* (Version v1.0.0) [Software]. Zenodo. https://doi.org/10.5281/zenodo.20021230

All authors contributed equally.

**BibTeX**

```bibtex
@software{saysuomi_asa_student_2026,
  author  = {Immonen, Laura and K{\"a}hk{\"o}nen, Aaron and Porola, Iida and
             Ratilainen, Miika and Savolainen, Aaro and Takala, Kim and
             Tonteri, Miika and Immonen, Riina and Phan, Nhan and
             von Zansen, Anna},
  title   = {{SaySuomi}: Automatic Speaking Assessment Student Release v1.0.0},
  year    = {2026},
  doi     = {10.5281/zenodo.20021230},
  version = {v1.0.0},
  note    = {Initial ASA module developed by the student team; later development continued in the main project. All authors contributed equally.}
}
```

## Project Structure

- `Assets/`: Unity app source files.
- `Packages/` and `ProjectSettings/`: Unity package and project configuration.
- `docs/`: ASA release documentation, historical plans, legacy server notes, and development notes.
- `MDD_Server/`: legacy/server-side project files kept for project context.

## Documentation

- [ASA release documentation](docs/asa_release/SaySuomi-ASA%20doc.pdf)
- [ASA front-end development plan](docs/ASA_frontend_plan.md)
- [Legacy CaptainA pronunciation server and API notes](docs/legacy_pronunciation_server.md)
- [Future development notes](docs/development_notes.md)

## Related Resources

- [ASA server: aalto-speech/dta-server](https://github.com/aalto-speech/dta-server)
- [Original pronunciation model on HuggingFace](https://huggingface.co/Usin2705/CaptainA_v0)
- [Legacy SaySvenska server example](https://github.com/Usin2705/SaySvenska/tree/main/Server)

## Contributors

<!-- readme: contributors -start -->
<table>
	<tbody>
		<tr>
            <td align="center">
                <a href="https://github.com/Usin2705">
                    <img src="https://avatars.githubusercontent.com/u/8575412?v=4" width="100;" alt="Usin2705"/>
                    <br />
                    <sub><b>Chi Nhan, Phan</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/AaroMKS">
                    <img src="https://avatars.githubusercontent.com/u/183390925?v=4" width="100;" alt="AaroMKS"/>
                    <br />
                    <sub><b>AaroMKS</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/choerubi">
                    <img src="https://avatars.githubusercontent.com/u/156372096?v=4" width="100;" alt="choerubi"/>
                    <br />
                    <sub><b>Iida Porola</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/ratilmii">
                    <img src="https://avatars.githubusercontent.com/u/32961917?v=4" width="100;" alt="ratilmii"/>
                    <br />
                    <sub><b>ratilmii</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/LauraImmonen">
                    <img src="https://avatars.githubusercontent.com/u/180306768?v=4" width="100;" alt="LauraImmonen"/>
                    <br />
                    <sub><b>Laura</b></sub>
                </a>
            </td>
            <td align="center">
                <a href="https://github.com/kahkaar">
                    <img src="https://avatars.githubusercontent.com/u/119842456?v=4" width="100;" alt="kahkaar"/>
                    <br />
                    <sub><b>Aaron</b></sub>
                </a>
            </td>
		</tr>
	<tbody>
</table>
<!-- readme: contributors -end -->

## License

The SaySuomi is licensed under the [GNU Affero General Public License, version 3 or later](https://github.com/Usin2705/CaptainA_unity/blob/main/LICENSE). Other related work to SaySuomi made by the authors (thesis work, journal articles, audio samples, pictures, videos ...) are licensed under a Creative Commons "Attribution-NonCommercial-ShareAlike 4.0 International" (BY-NC-SA 4.0) [license](https://creativecommons.org/licenses/by-nc-sa/4.0/).

Other works not made by the authors are licensed accordingly to their respective owners:

- The authors of Oma Suomi 1: Kristiina Kuparinen, Terhi Tapaninen and Finn Lectura have given us permission to use the text in Oma Suomi 1 to create the flashcard for the SaySuomi app.
- Anki is licensed under AGPL3.
- SuperMemo2 is open to the public: Algorithm SM-2, (C) Copyright SuperMemo World, 1991. [https://www.supermemo.com](https://www.supermemo.com).
- The side picture illustrations are created by Aino Huhtaniemi ([https://ainohuhtaniemi.com/](https://ainohuhtaniemi.com/)), and she gave her permission to use and modify her original illustrations for the SaySuomi app.
- Some icons used in the application are from Google under Apache License 2.0.
- Photo illustrations and some of the videos were made with the contribution of Aija Elg and Noora Heikiö from Aalto University Language Centre.
- Some audio samples are from Aalto University Language Centre.
- Some audio samples are from Common Voice 11.0, licensed under [Creative Commons Zero 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- Some audio samples and text examples are from LibriVox under Public Domain.
- We are grateful to Apollo Ailus and Kia Raitanen for their contributions to user research and engagement, and to Aalo Kailu, who designed the original user interface of the app.
- We are grateful to the 2026 ASA student team, Laura Immonen, Aaron Kähkönen, Iida Porola, Miika Ratilainen, Aaro Savolainen, Kim Takala, and Miika Tonteri, for developing the initial Automatic Speaking Assessment module v1.0.0.
