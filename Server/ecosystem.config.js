module.exports = {
  /**
   * Application configuration section
   * http://pm2.keymetrics.io/docs/usage/application-declaration/
   */
  apps : [
    {
      name      : 'signal',
      script    : 'npm', 
      args      : 'start',
      watch     : true
    }
  ]
};
