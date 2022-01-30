import Vue from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
import { BootstrapVue, IconsPlugin } from "bootstrap-vue";

import "bootstrap/dist/css/bootstrap.css";
import "bootstrap-vue/dist/bootstrap-vue.css";

import './app.scss'

import { library } from '@fortawesome/fontawesome-svg-core'
import { faCheckCircle, faSpinner, faEdit } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import ManagerService from '@/services/ManagerService';

library.add(faCheckCircle, faSpinner, faEdit)

Vue.component('font-awesome-icon', FontAwesomeIcon)

Vue.config.productionTip = false

Vue.use(BootstrapVue);
Vue.use(IconsPlugin);

export const EventBus = new Vue();
export const Manager = new ManagerService();
Manager.startConnectionToServer();

new Vue({
  router,
  store,
  render: h => h(App)
}).$mount('#app')
