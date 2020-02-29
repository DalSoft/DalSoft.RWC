const Glob = require("glob");
const Path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const TerserJSPlugin = require('terser-webpack-plugin');
const OptimizeCSSAssetsPlugin = require("optimize-css-assets-webpack-plugin");

const razorPagesRoot = "./Pages/";
const htmlPlugins = devMode => generateHtmlPlugins(devMode);

function generateHtmlPlugins(devMode) {

    const allEntries = Glob.sync(`${razorPagesRoot}**/*.{js,css}`);

    const razorTemplates = allEntries.reduce((uniqueEntries, item) => {

        const folder = item.substring(0, item.lastIndexOf("/") + 1);
        const fileName = item.substring(item.lastIndexOf("/") + 1, Path.length).replace(".js", "").replace(".css", "");
        const chunk = `${folder.replace(razorPagesRoot, "")}${fileName}`;

        if (uniqueEntries.filter(e => e.options.chunks[0] === chunk).length > 0)
            return uniqueEntries;

        uniqueEntries.push(new HtmlWebpackPlugin({
            chunks: [chunk],
            template: "./_Webpack/WebpackRazorComponents.ejs",                              // Razor components must start with uppercase
            filename: `${folder.replace(razorPagesRoot, "../../_Webpack/GeneratedRazorComponents/")}${fileName.replace(/[a-zA-Z]{1}/, (c, i) => c.toUpperCase())}.razor`,
            minify: false,
            inject: false
        }));

        return uniqueEntries;
    }, []);

    const vendors = new HtmlWebpackPlugin({
        chunks: ["vendors"],
        template: "./_Webpack/WebpackRazorComponents.ejs",
        filename: "../../_Webpack/GeneratedRazorComponents/Vendors.razor",
        minify: false,
        inject: false
    });

    razorTemplates.push(vendors);

    const allChunks = razorTemplates.reduce((chunks, htmlWebpackPlugin) => {
        chunks.push(htmlWebpackPlugin.options.chunks[0]);
        return chunks;
    }, []);

    const componentsCSharp = new HtmlWebpackPlugin({
        devMode: devMode,
        proj: Glob.sync('*.csproj')[0] ? Glob.sync('*.csproj')[0] : '',
        chunks: allChunks,
        template: "./_Webpack/ComponentsCSharp.ejs",
        filename: "../../_Webpack/GeneratedRazorComponents/_Components.cs",
        minify: false,
        inject: false
    });

    razorTemplates.push(componentsCSharp);

    return razorTemplates;
}

module.exports = (env, arg) => {

    console.log(`mode: ${arg.mode}`);
    if (process.env.PORT)
        console.log('Starting the development server'); // required HACK for spa.UseReactDevelopmentServer

    const devMode = arg.mode !== 'production';

    return {
        resolve: {
            symlinks: false
        },
        //devServer: {
        //    contentBase: "wwwroot/dist",
        //    publicPath: "/dist/",
        //    inline: false,
        //    proxy: {
        //        '/': {
        //            target: 'https://localhost:44327',
        //            secure: false
        //        }
        //    }
        //},
        entry: Glob.sync(`${razorPagesRoot}**/*.{js,css}`).reduce(
            (entries, entry) => {
                const objectKey = entry.replace(razorPagesRoot, "").replace(".js", "").replace(".css", "");
                if (entries[objectKey]) {
                    entries[objectKey] = [...entries[objectKey], entry];
                    return entries;
                }
                return Object.assign(entries, { [objectKey]: [entry] });
            }, {}),
        output: {
            path: Path.resolve(__dirname, "wwwroot/dist"),
            filename: devMode ? "js/[name].js" : "js/[name].[Contenthash:8].js"
        },
        optimization: {
            minimizer: [new TerserJSPlugin({}), new OptimizeCSSAssetsPlugin({})],
            splitChunks: {
                cacheGroups: {
                    commons: {
                        test: /[\\/]node_modules[\\/]/,
                        name: "vendors",
                        chunks: "all"
                    }
                }
            }
        },
        module: {
            rules: [
                {
                    test: /\.(js)$/,
                    include: Path.resolve(__dirname, razorPagesRoot),
                    use: ["cache-loader", "babel-loader"]
                },
                {
                    test: /\.css$/,
                    include: Path.resolve(__dirname, razorPagesRoot),
                    use: [MiniCssExtractPlugin.loader, "css-loader"]
                },
                {
                    test: /\.(png|svg|jpg|gif)$/,
                    include: Path.resolve(__dirname, razorPagesRoot),
                    use: [
                        {
                            loader: "file-loader",
                            options: {
                                name: devMode ? "assets/images/[path][name].[ext]" : "assets/images/[path][name].[Contenthash:8].[ext]",
                                publicPath: url => url.replace("assets/images/Pages/", "/dist/assets/images/"),
                                outputPath: url => url.replace("assets/images/Pages/", "assets/images/")
                            }
                        }
                    ]
                },
                {
                    test: /\.(woff|woff2|eot|ttf|otf)$/,
                    include: Path.resolve(__dirname, razorPagesRoot),
                    use: [
                        {
                            loader: "file-loader",
                            options: {
                                name: devMode ? "assets/fonts/[path][name].[ext]" : "assets/fonts/[path][name].[Contenthash:8].[ext]",
                                publicPath: url => url.replace("assets/fonts/Pages/", "/dist/assets/fonts/"),
                                outputPath: url => url.replace("assets/fonts/Pages/", "assets/fonts/")
                            }
                        }
                    ]
                }
            ]
        },

        plugins: [
            new CleanWebpackPlugin({
                cleanOnceBeforeBuildPatterns: ["**/*", "../../_Webpack/GeneratedRazorComponents/*"],
                dangerouslyAllowCleanPatternsOutsideProject: true,
                dry: false
            }),
            new MiniCssExtractPlugin({
                filename: devMode ? "css/[name].css" : "css/[name].[Contenthash:8].css"
            })
        ]
            .concat(htmlPlugins(devMode))
    }
};